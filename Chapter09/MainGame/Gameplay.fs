module PracticalFSharp.Gameplay

open Microsoft.Xna.Framework

// Seconds
[<Measure>]
type s

// Humans
[<Measure>]
type h

// Meters
[<Measure>]
type m

// Pixels
[<Measure>]
type pix

type City =
    { Name : string
      Population : float32<h>
      Position : TypedVector2<m>
    }

type Submarine =
    { Position : TypedVector2<m>
      Period : float32<s>   // Initial amount of time between each missile launch.
    }
with
    static member SurfaceTime = 0.5f<s> // Initial delay between becoming visible and launching (in seconds).

type World =
    { Cities : City[]
      Submarines : Submarine[]
      TopLeft : TypedVector2<m>
      BottomRight : TypedVector2<m>
    }

type Missile =
    { Position : TypedVector2<m>
      Velocity : TypedVector2<m/s>
      Target : TypedVector2<m>
    }
with
    static member Speed = 3000.0f * 340.0f<m/s> // ~ MACH 3000

type Explosion =
    { Position : TypedVector2<m>
      Time : float32<s>
    }
with
    static member MaxTime = 0.5f<s>
    static member Radius = 100000.0f<m> // 100km
    static member ShieldRadius = 100000.0f<m> // 100km

type WorldState =
    { Survivors : float32<h>[]  // Population left in each city
      Submarines : float32<s>[] // Time since last launch.
      Missiles : Missile list
      Explosions : Explosion list
      Shields : Explosion list
      Difficulty : float32 // 1.0 means normal, increases with time.
      DifficultyTimeLeft : float32<s> // Time left before difficulty increases.
      Score : float32
      DefenseCoolDown : float32<s> // Amount of time before player can shoot again.
      ReticlePosition : TypedVector2<pix>
    }
with
    static member DefenseMaxCoolDown = 1.0f<s>
    static member DifficultyIncreasePeriod = 30.0f<s>

type Order =
    | DeployShield of TypedVector2<m> // World coordinates (in meters).
    | MoveReticle of TypedVector2<pix> // Screen coordinates (in pixels).

type Event =
    | PlayExplosion
    | PlayShield

let update (random : System.Random) (world : World) (dt : float32<s>) orders state =
    let decr v = v - dt
    let incr v = v + dt

    // Check and execute orders
    let state, playShieldEvent =
        orders
        |> List.fold (fun (state, ev) order ->
            match order with
            | DeployShield pos ->
                if state.DefenseCoolDown > 0.0f<s> then
                    state, ev
                else
                    { state with
                        Shields =
                            { Position = pos
                              Time = 0.0f<s> } :: state.Shields
                        DefenseCoolDown = WorldState.DefenseMaxCoolDown
                    },
                    PlayShield :: ev
            | MoveReticle pos ->
                { state with ReticlePosition = pos }, ev
            ) (state, [])

    // Update times
    let subs =
        state.Submarines
        |> Array.map incr
    
    let explosions =
        state.Explosions
        |> List.map (fun ex -> { ex with Time = incr ex.Time })

    let shields =
        state.Shields
        |> List.map (fun ex -> { ex with Time = incr ex.Time })

    let diffTimeLeft = decr state.DifficultyTimeLeft

    let defCoolDown = decr state.DefenseCoolDown

    // Remove "dead" items
    let explosions =
        explosions
        |> List.filter (fun ex -> ex.Time < Explosion.MaxTime)

    let shields =
        shields
        |> List.filter (fun ex -> ex.Time < Explosion.MaxTime)

    // Difficulty update
    let difficulty, diffTimeLeft =
        if diffTimeLeft < 0.0f<s> then
            state.Difficulty * 1.1f, diffTimeLeft + WorldState.DifficultyIncreasePeriod
        else
            state.Difficulty, diffTimeLeft

    // Launch new missiles
    let newMissiles =
        Array.zip subs world.Submarines
        |> Array.choose (fun (timeSinceLastLaunch, submarine) ->
            if timeSinceLastLaunch * difficulty > submarine.Period then
                let city = random.Next(world.Cities.Length)
                let target = world.Cities.[city].Position
                let direction =
                    target - submarine.Position
                    |> TypedVector.normalize2
                Some
                    { Position = submarine.Position
                      Target = target
                      Velocity = state.Difficulty * Missile.Speed * direction 
                    }
            else
                None)
        |> List.ofArray

    let subs =
        Array.zip subs world.Submarines
        |> Array.map (fun (timeSinceLastLaunch, submarine) ->
            if timeSinceLastLaunch * difficulty > submarine.Period then
                0.0f<s>
            else
                timeSinceLastLaunch)

    // Update missile positions
    let missiles =
        state.Missiles
        |> List.map (fun missile ->
            { missile with
                Position = missile.Position + dt * missile.Velocity
            })

    let missiles =
        List.append missiles newMissiles

    // Detonate missiles that are close to cities
    let missiles, detonations =
        missiles
        |> List.fold (fun (missiles, detonations) missile ->
            let newPos = missile.Position + dt * missile.Velocity
            let dist = TypedVector.len2 (missile.Target - missile.Position)
            let dist2 = TypedVector.len2 (missile.Target - newPos)
            if dist2 > dist then
                missiles, { Position = missile.Position; Time = 0.0f<s> } :: detonations
            else
                missile :: missiles, detonations
            ) ([], [])

    // Destroy missiles that are hit by shields
    let missiles, destroyed =
        missiles
        |> List.fold (fun (missiles, destroyed) missile ->
            let isDestroyed =
                shields
                |> List.exists (fun shield -> TypedVector.len2 (missile.Position - shield.Position) < Explosion.ShieldRadius)
            if isDestroyed then
                missiles, { Position = missile.Position; Time = 0.0f<s> } :: destroyed
            else
                missile :: missiles, destroyed
            ) ([], [])

    // Kill population that are within explosions
    let newExplosions = List.append detonations destroyed
    let survivors =
        Array.zip world.Cities state.Survivors
        |> Array.map (fun (city, survivors) ->
            newExplosions
            |> List.fold (fun survivors explosion ->
                let distance = TypedVector.len2 (city.Position - explosion.Position)
                let relativeCasualties =
                    if distance > Explosion.Radius then
                        0.0f
                    else
                        1.0f - distance/Explosion.Radius
                let casualties = city.Population * relativeCasualties
                max (survivors - casualties) 0.0f<h>
                ) survivors
        )

    let points =
        float32 (List.length destroyed) * difficulty * 1000.0f

    let playExplosionEvent =
        if List.isEmpty newExplosions then
            []
        else
            [PlayExplosion]

    let events = playShieldEvent @ playExplosionEvent

    let state =
        { Survivors = survivors
          Submarines = subs
          Missiles = missiles
          Explosions = List.append explosions newExplosions
          Shields = shields
          Difficulty = difficulty
          DifficultyTimeLeft = diffTimeLeft
          Score = state.Score + points
          DefenseCoolDown = defCoolDown
          ReticlePosition = state.ReticlePosition
        }
    
    state, events