module PracticalFSharp.Rendering

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Audio

open Gameplay

let drawRectangleCentered (sb : SpriteBatch) (texture : Texture2D) (pos : TypedVector2<pix>) (srcRect : Rectangle) =
    let w = float32 srcRect.Width
    let h = float32 srcRect.Height
    let destRect = Rectangle(int (pos.v.X - w / 2.0f), int (pos.v.Y - h / 2.0f), srcRect.Width, srcRect.Height)
    sb.Draw(texture, destRect, System.Nullable<_>(srcRect), Color.White)

let drawRectangleRotated (sb : SpriteBatch) (texture : Texture2D) (pos : TypedVector2<pix>) angle (srcRect : Rectangle) =
    let w = float32 srcRect.Width
    let h = float32 srcRect.Height
    let destRect = Rectangle(int pos.X, int pos.Y, srcRect.Width, srcRect.Height)
    let origin = Vector2(float32 srcRect.X + w/2.0f, float32 srcRect.Y + h/2.0f)
    sb.Draw(texture, destRect, System.Nullable<_>(srcRect), Color.White, angle, origin, SpriteEffects.None, 0.0f)

let drawTextCentered (sb : SpriteBatch) (font : SpriteFont) (pos : TypedVector2<pix>) (text : string) =
    let textSize = font.MeasureString(text)
    let pos = new TypedVector2<pix>(pos.v - 0.5f * textSize)
    sb.DrawString(font, text, pos.v, Color.BlueViolet)

type Resources =
    { Background : Texture2D
      Explosion : Texture2D
      Shield : Texture2D
      City : Texture2D
      Missile : Texture2D
      Submarine : Texture2D
      Surfacing : Texture2D
      EnabledPointer : Texture2D
      DisabledPointer : Texture2D
      ScoreFont : SpriteFont
      ExplosionSound : SoundEffect
      MissileLaunch : SoundEffect
      ShieldSound : SoundEffect
      SpriteBatch : SpriteBatch
    }

let render (soundInstances : SoundEffectInstances) (gd : GraphicsDevice) (rsc : Resources) (world : World) (oldState : WorldState, state : WorldState) events =
    // Explosion and shield sounds
    for ev in events do
        match ev with
        | PlayExplosion ->
            soundInstances.Play(rsc.ExplosionSound)
        | PlayShield ->
            soundInstances.Play(rsc.ShieldSound)

    // Graphics
    let transform1 (worldMin : float32<m>) (worldWidth : float32<m>) (screenMin : float32<pix>) (screenWidth : float32<pix>) x =
        let x = (x - worldMin) / worldWidth
        let x = screenMin + x * screenWidth
        x
    let transformX =
        transform1 world.TopLeft.X (world.BottomRight.X - world.TopLeft.X) (1.0f<pix> * float32 gd.Viewport.TitleSafeArea.Left) (1.0f<pix> * float32 gd.Viewport.TitleSafeArea.Width)
    let transformY =
        transform1 world.TopLeft.Y (world.BottomRight.Y - world.TopLeft.Y) (1.0f<pix> * float32 gd.Viewport.TitleSafeArea.Top) (1.0f<pix> * float32 gd.Viewport.TitleSafeArea.Height)
    let transform (v : TypedVector2<m>) =
        let x = transformX v.X
        let y = transformY v.Y
        TypedVector2(x, y)

    let getFrameRectangle (texture : Texture2D) k =
        // We assume sprite sheets use horizontal strips of squares.
        let squareSize = texture.Height
        let numFrames = texture.Width / squareSize
        let k' = int <| float32 numFrames * k
        let i = (numFrames - 1) |> min k' |> max 0
        let x = squareSize * i
        Rectangle(x, 0, squareSize, squareSize)

    let (|Diving|UnderWater|Surfacing|OnSurface|) (timeBetweenLaunches : float32<s>, timeSinceLaunch : float32<s>) =
        let timeBetweenLaunches = timeBetweenLaunches / state.Difficulty
        let progress = timeSinceLaunch / timeBetweenLaunches

        // Relative times between 0.0 and 1.0, sum must be 1.0 (100%)
        let divingTime = 0.2f
        let underWaterTime = 0.5f
        let surfacingTime = 0.2f
        let surfaceTime = 0.1f

        if progress < divingTime then
            Diving (progress / divingTime)
        elif progress < divingTime + underWaterTime then
            UnderWater
        elif progress < divingTime + underWaterTime + surfacingTime then
            Surfacing ((progress - divingTime - underWaterTime) / surfacingTime)
        else
            OnSurface ((progress - divingTime - underWaterTime - surfacingTime) / surfaceTime)

    try
        rsc.SpriteBatch.Begin()

        // World map.
        rsc.SpriteBatch.Draw(rsc.Background, gd.Viewport.TitleSafeArea, Color.White)

        // Cities.
        for city, survivors in Array.zip world.Cities state.Survivors do
            rsc.City.Bounds
            |> drawRectangleCentered rsc.SpriteBatch rsc.City (transform city.Position)

        // City populations.
        for city, survivors in Array.zip world.Cities state.Survivors do            
            let population = sprintf "%d" (max (int survivors) 0)
            drawTextCentered rsc.SpriteBatch rsc.ScoreFont (TypedVector2<pix>(0.0f<pix>, 1.0f<pix> * float32 -rsc.City.Bounds.Height) + transform city.Position) population

        // Submarines.
        for submarine, timeSinceLaunch in Array.zip world.Submarines state.Submarines do
            match (submarine.Period, timeSinceLaunch) with
            | OnSurface t ->
                getFrameRectangle rsc.Submarine t
                |> drawRectangleCentered rsc.SpriteBatch rsc.Submarine (transform submarine.Position)
            | Diving t ->
                getFrameRectangle rsc.Surfacing (1.0f - t)
                |> drawRectangleCentered rsc.SpriteBatch rsc.Surfacing (transform submarine.Position)
            | Surfacing t ->
                getFrameRectangle rsc.Surfacing t
                |> drawRectangleCentered rsc.SpriteBatch rsc.Surfacing (transform submarine.Position)
            | UnderWater -> ()

        // Missiles. TODO animation.
        for missile in state.Missiles do
            let angle = atan2 missile.Velocity.Y missile.Velocity.X
            getFrameRectangle rsc.Missile 0.0f
            |> drawRectangleRotated rsc.SpriteBatch rsc.Missile (transform missile.Position) angle

        // Explosions.
        for explosion in state.Explosions do
            getFrameRectangle rsc.Explosion (explosion.Time / Explosion.MaxTime)
            |> drawRectangleCentered rsc.SpriteBatch rsc.Explosion (transform explosion.Position)

        // Shields.
        for shield in state.Shields do
            getFrameRectangle rsc.Shield (shield.Time / Explosion.MaxTime)
            |> drawRectangleCentered rsc.SpriteBatch rsc.Shield (transform shield.Position)

        // Reticle.
        let reticle, frame =
            if state.DefenseCoolDown > 0.0f<s> then
                rsc.DisabledPointer, 1.0f - state.DefenseCoolDown / WorldState.DefenseMaxCoolDown
            else
                rsc.EnabledPointer, 0.0f
        getFrameRectangle reticle frame
        |> drawRectangleCentered rsc.SpriteBatch reticle state.ReticlePosition

        // Score.
        let score = sprintf "%05d" (int state.Score)
        let scoreSize = rsc.ScoreFont.MeasureString(score)
        let posX = float32 gd.Viewport.TitleSafeArea.Right - scoreSize.X
        let posY = float32 gd.Viewport.TitleSafeArea.Top
        rsc.SpriteBatch.DrawString(rsc.ScoreFont, score, Vector2(posX, posY), Color.Gold)

    finally
        rsc.SpriteBatch.End()
