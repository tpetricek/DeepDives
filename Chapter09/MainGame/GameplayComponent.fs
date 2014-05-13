module PracticalFSharp.GameplayComponent

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Microsoft.Xna.Framework.Audio

open PracticalFSharp
open PracticalFSharp.Gameplay
open PracticalFSharp.Rendering
open PracticalFSharp.InputStateMachine
open PracticalFSharp.PressStart

type Component(game, controlDevice) =
    inherit DrawableGameComponent(game)

    let pictureWidth = 1200.0f<pix>
    let pictureHeight = 1084.0f<pix>
    let mapWidth = 10000000.0f<m> // 10,000 km
    let mapHeight = mapWidth * pictureHeight / pictureWidth

    let newPos(x, y) =
        TypedVector2<m>(mapWidth * x / pictureWidth, mapHeight * y / pictureHeight)

    let cities =
        [| { Name = "Paris"; Population = 2000000.0f<h>; Position = newPos(369.0f<pix>, 615.0f<pix>) }
           { Name = "Madrid"; Population = 3300000.0f<h>; Position = newPos(202.0f<pix>, 790.0f<pix>) }
           { Name = "Berlin"; Population = 3300000.0f<h>; Position = newPos(578.0f<pix>, 532.0f<pix>) }
           { Name = "Stockholm"; Population = 1000000.0f<h>; Position = newPos(655.0f<pix>, 351.0f<pix>) }
           { Name = "London"; Population = 8000000.0f<h>; Position = newPos(351.0f<pix>, 523.0f<pix>) }
        |]

    let submarines =
        [| { Position = newPos(46.0f<pix>, 46.0f<pix>); Period = 5.0f<s> }
           { Position = newPos(1020.0f<pix>, 1071.0f<pix>); Period = 8.0f<s> }
           { Position = newPos(981.0f<pix>, 764.0f<pix>); Period = 7.0f<s> }
        |]

    let world =
        { Cities = cities
          Submarines = submarines
          TopLeft = TypedVector2.Zero
          BottomRight = TypedVector2(mapWidth, mapHeight) }

    let random = System.Random()
    let newState =
        ref { Survivors = cities |> Array.map (fun city -> city.Population)
              Submarines = submarines |> Array.map (fun sub -> sub.Period * (float32 <| random.NextDouble()))
              Missiles = []
              Explosions = []
              Shields = []
              Difficulty = 1.0f
              DifficultyTimeLeft = WorldState.DifficultyIncreasePeriod
              Score = 0.0f
              DefenseCoolDown = 0.0f<s>
              ReticlePosition = TypedVector2.Zero
            }
    let oldState = ref (!newState)

    let events = ref []

    let content = ref None

    let soundInstances = new SoundEffectInstances()

    let gameOver = new Event<int>()

    let isPaused = ref false

    let flipPaused() =
        isPaused := not <| !isPaused

        if !isPaused then
            soundInstances.PauseAll()
        else
            soundInstances.ResumeAll()

#if KEYBOARD_ENABLED
    let escPressed (ks : KeyboardState) = ks.IsKeyDown(Keys.Escape)
    let escReleased (ks : KeyboardState) = ks.IsKeyUp(Keys.Escape)
    let keyboardPausing0 = waitReleased escPressed escReleased ignore
    let keyboardPausing = ref keyboardPausing0
#endif

#if GAMEPAD_ENABLED
    let startPressed (gs : GamePadState) = gs.IsButtonDown(Buttons.Start)
    let startReleased (gs : GamePadState) = gs.IsButtonUp(Buttons.Start)
    let gamepadPausing0 = waitReleased startPressed startReleased ignore
    let gamepadPausing = ref gamepadPausing0
#endif

    member private this.GetOrders(dt : float32<s>, state : WorldState) =
        let orders = []
        let moveReticle = []
        let getWorldPos(v : TypedVector2<pix>) =
            let clamp x = MathHelper.Clamp(x, 0.0f, 1.0f)
            let x = mapWidth * clamp (v.X / (1.0f<pix> * float32 this.GraphicsDevice.Viewport.TitleSafeArea.Width))
            let y = mapHeight * clamp (v.Y / (1.0f<pix> * float32 this.GraphicsDevice.Viewport.TitleSafeArea.Height))
            TypedVector2(x, y)

#if MOUSE_ENABLED
        let orders, moveReticle =
            match controlDevice with
            | KeyboardAndMouse ->
                let mouseState = Mouse.GetState()
                let newPos =
                    TypedVector2(1.0f<pix> * float32 mouseState.X, 1.0f<pix> * float32 mouseState.Y)
                let orders =
                    if mouseState.LeftButton = ButtonState.Pressed then
                        let pos = getWorldPos(newPos)
                        DeployShield(pos) :: orders
                    else
                        orders
                orders, [MoveReticle newPos]
            | Gamepad controllingPlayer ->
                orders, []
#endif
#if GAMEPAD_ENABLED
        let orders, moveReticle =
            match controlDevice with
            | Gamepad controllingPlayer ->
                let padState = GamePad.GetState(controllingPlayer)
                if padState.IsConnected then
                    let r (x :float32) (y : float32) =
                        sqrt (x * x + y * y)
                    let smooth x =
                        x * x * x
                    let r = r padState.ThumbSticks.Left.X padState.ThumbSticks.Left.Y
                    let deadzone = 0.1f
                    let moveReticle =
                        if r > deadzone then
                            let alpha = atan2 padState.ThumbSticks.Left.Y padState.ThumbSticks.Left.X
                            let dx = r * cos alpha
                            let dy = r * sin alpha
                            let newPos =
                                let nominalWidth = 1024.0f
                                let sensitivity = 500.0f<pix/s> * (float32 this.GraphicsDevice.Viewport.Width) / nominalWidth
                                state.ReticlePosition + sensitivity * dt * TypedVector2<1>(dx, -dy)
                            [MoveReticle newPos]
                        else
                            []
                    if padState.Buttons.A = ButtonState.Pressed then
                        let pos = 
                            match moveReticle with
                            | [MoveReticle newPos] ->
                                getWorldPos newPos                        
                            | _ ->
                                getWorldPos state.ReticlePosition
                        DeployShield(pos) :: orders, moveReticle
                    else
                        orders, moveReticle
                else
                    orders, []
            | KeyboardAndMouse ->
                orders, moveReticle
#endif
        moveReticle @ orders

    member private this.UpdateIsPaused() =
#if KEYBOARD_ENABLED
        match !keyboardPausing with
        | Active update ->
            keyboardPausing := Keyboard.GetState() |> update
        | Done () ->
            flipPaused()
            keyboardPausing := keyboardPausing0

#endif
#if GAMEPAD_ENABLED
        match controlDevice, !gamepadPausing with
        | Gamepad controllingPlayer, Active update ->
            gamepadPausing := GamePad.GetState(controllingPlayer) |> update
        | _, Done () ->
            flipPaused()
            gamepadPausing := gamepadPausing0
        | KeyboardAndMouse, _ ->
            ()
#endif
        
    override this.LoadContent() =
        let background = game.Content.Load("europe_outline")
        let city = game.Content.Load("city")
        let cursor = game.Content.Load("cursor")
        let recharging = game.Content.Load("recharging")
        let missile = game.Content.Load("missile")
        let explosion = game.Content.Load("nuke")
        let shield = game.Content.Load("shield")
        let submarine = game.Content.Load("submarine")
        let surfacing = game.Content.Load("surfacing")
        let explosionSound = game.Content.Load("explosion")
        let font = game.Content.Load("font")
        let shieldSound = game.Content.Load("lasershot")
        let missileLaunch = game.Content.Load("med_whoosh_00")
        let spriteBatch = new SpriteBatch(this.GraphicsDevice)        
        content :=
            Some { Background = background
                   Explosion = explosion
                   Shield = shield
                   City = city
                   Missile = missile
                   Submarine = submarine
                   Surfacing = surfacing
                   EnabledPointer = cursor
                   DisabledPointer = recharging
                   ScoreFont = font
                   ExplosionSound = explosionSound
                   MissileLaunch = missileLaunch
                   ShieldSound = shieldSound
                   SpriteBatch = spriteBatch
                 }

    override this.Update(gt) =
        soundInstances.Update()
        this.UpdateIsPaused()
        if not <| !isPaused then
            let dt = 1.0f<s> * float32 gt.ElapsedGameTime.TotalSeconds
            let orders = this.GetOrders(dt, !newState)
            let state2, events2 = update random world dt orders !newState
            events := events2
            oldState := !newState
            newState := state2
            if this.Survivors <= 0.0f<h> then
                gameOver.Trigger(int state2.Score)

    override this.Draw(gt) =
        match !content with
        | Some content ->
            if not <| !isPaused then
                render soundInstances this.GraphicsDevice content world (!oldState, !newState) !events
            else
                let center =
                    let view = this.GraphicsDevice.Viewport
                    let x = view.X + view.Width / 2
                    let y = view.Y + view.Height / 2
                    TypedVector2(1.0f<pix> * float32 x, 1.0f<pix> * float32 y)
                try
                    content.SpriteBatch.Begin()
                    drawTextCentered content.SpriteBatch content.ScoreFont center "PAUSED"
                finally
                    content.SpriteBatch.End()
        | None ->
            ()

    member this.Survivors =
        (!newState).Survivors |> Array.sum

    member this.GameOver =
        gameOver.Publish

    override this.Dispose(disposing) =
        base.Dispose(disposing)
        if disposing then
            soundInstances.Dispose()