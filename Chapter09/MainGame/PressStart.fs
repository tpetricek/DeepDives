module PracticalFSharp.PressStart

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input

open PracticalFSharp.InputStateMachine

type Resources =
    { Font : SpriteFont
      Batch : SpriteBatch }

type ControlDevice =
    | KeyboardAndMouse
    | Gamepad of PlayerIndex

type PressStart(game, content : Content.ContentManager) =
    inherit DrawableGameComponent(game)

    let mutable resources = None

    let players =
        [| 0 .. 3 |]
        |> Array.map(fun i -> enum<PlayerIndex> i)

#if GAMEPAD_ENABLED
    let getControllingPlayerFromGamePad =
        let buttons = [| Buttons.A; Buttons.Start |]

        let hasSomeButtonPressed (gamepad : GamePadState) =
            buttons
            |> Array.exists (fun btn -> gamepad.IsButtonDown(btn))

        let someButtonPressed (gamepads : GamePadState[]) =
            gamepads
            |> Array.exists hasSomeButtonPressed

        let getControllingPlayer (gamepads : GamePadState[]) =
            let idx =
                gamepads
                |> Array.findIndex hasSomeButtonPressed
            players.[idx]

        let allButtonsReleased (gamepads : GamePadState[]) =
            gamepads
            |> Array.forall (fun gamepad ->
                buttons
                |> Array.forall (fun btn -> gamepad.IsButtonUp(btn))
            )

        waitReleased someButtonPressed allButtonsReleased getControllingPlayer
        |> ref
#endif

#if KEYBOARD_ENABLED
    let getControllingPlayerFromKeyboard =
        let keys = [| Keys.Enter; Keys.Space |]

        let isKeyPressed (state : KeyboardState) =
            keys
            |> Array.exists(fun k -> state.IsKeyDown(k))

        let getControllingPlayer _ = ()
            
        let areKeysReleased (state : KeyboardState) =
            keys
            |> Array.forall(fun k -> state.IsKeyUp(k))

        waitReleased isKeyPressed areKeysReleased getControllingPlayer
        |> ref
#endif

    let startPressed = new Event<ControlDevice>()

    member this.StartPressed = startPressed.Publish

    override this.LoadContent() =
        let font = content.Load("font")
        let batch = new SpriteBatch(this.GraphicsDevice)
        resources <- Some { Font = font; Batch = batch }

    override this.Update(gt) =
        let result = None

#if GAMEPAD_ENABLED
        let result =
            match !getControllingPlayerFromGamePad with
            | Active update ->
                let nextState =
                    players
                    |> Array.map (fun pi -> GamePad.GetState(pi))
                    |> update
                getControllingPlayerFromGamePad := nextState
                None
            | Done result ->
                result |> Gamepad |> Some
#endif

#if KEYBOARD_ENABLED        
        let result =
            match result, !getControllingPlayerFromKeyboard with
            | Some _, _ ->
                result
            | None, Active update ->
                let nextState =
                    Keyboard.GetState()
                    |> update
                getControllingPlayerFromKeyboard := nextState
                None
            | None, Done _ ->
                Some KeyboardAndMouse
#endif

        match result with
        | None ->
            ()
        | Some result ->
            startPressed.Trigger(result)


    override this.Draw(gt) =
        let backgroundColor = Color.LightGray
        let foregroundColor = Color.Navy
        base.GraphicsDevice.Clear(backgroundColor)
        match resources with
        | Some rsc ->
            let isVisible = gt.TotalGameTime.Milliseconds % 1000 < 700
            if isVisible then
                let text = "Press start"
                let pos =
                    let size = rsc.Font.MeasureString(text)
                    let safe = this.GraphicsDevice.Viewport.TitleSafeArea
                    let posx = safe.Left + (safe.Width - int size.X) / 2
                    let posy = safe.Top + (safe.Height - int size.Y) / 2
                    Vector2(float32 posx, float32 posy)
                try
                    rsc.Batch.Begin()
                    rsc.Batch.DrawString(rsc.Font, text, pos, foregroundColor)
                finally
                    rsc.Batch.End()
        | None ->
            ()

    override this.Dispose(disposeManaged) =
        base.Dispose(disposeManaged)
        if disposeManaged then
            match resources with
            | Some { Batch = batch } -> batch.Dispose()
            | None -> ()
