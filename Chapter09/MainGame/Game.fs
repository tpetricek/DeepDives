namespace PracticalFSharp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.GamerServices

open Dialogs
open PressStart
open ScoreComponent

type Game() as this =
    inherit Microsoft.Xna.Framework.Game()

    let graphics = new GraphicsDeviceManager(this)
    do this.Content.RootDirectory <- "Content"

    override this.Initialize() =
        // The gamer services component is a requirement before performing I/O using the XNA storage API.
        let gamerServices = new GamerServicesComponent(this)
        this.Components.Add(gamerServices)

        // Our component which allows the user to choose a controller to play the game.
        let pressStart = new PressStart.PressStart(this, this.Content)
        this.Components.Add(pressStart)

        let afterGamePlay
            (controlDevice : ControlDevice)
            (device : Storage.StorageDevice)
            score =
            async {
                let! scores = AsyncStorage.loadScores device "SampleGame" "scores"
                match scores with
                | AsyncStorage.IOResult.Successful scores ->
                    let name =
                        let gsm = GamerServices.Gamer.SignedInGamers
                        match controlDevice with
                        | KeyboardAndMouse ->
                            "Anonymous"
                        | Gamepad controllingPlayer ->
                            match gsm.[controllingPlayer] with
                            | null -> "Anonymous"
                            | player -> player.DisplayName
                    let board =
                        { NewScore = (name, score)
                          Scores = scores.GetAsPairs()
                        }
                    // Create and register the score component.
                    let scoresComponent =
                        new ScoreComponent(this, this.Content, board)
                    this.Components.Add(scoresComponent)
                    // Update the score board with the new score.
                    scores.InsertScore(AsyncStorage.NamedScore(Name = name, Score = score))
                    scores.Truncate(10);
                    let! succeeded = AsyncStorage.saveScores device "SampleGame" "scores" scores
                    if not succeeded then
                        do! doWhenGuideNoLongerVisible <|
                            showError "Failed to save scores"
                | AsyncStorage.IOResult.BadData ->
                    do! doWhenGuideNoLongerVisible <|
                        showError "Score data is corrupted"
                | AsyncStorage.IOResult.DeviceDisconnected ->
                    do! doWhenGuideNoLongerVisible <|
                        showAlert "Alert" "Storage device was disconnected"
            }

        let mainTask (controlDevice : ControlDevice) =
            async {
                let! maybeDevice = AsyncStorage.forceShowSelector
                match maybeDevice with
                | None ->
                    do! doWhenGuideNoLongerVisible
                        <| showAlert
                            "Alert"
                            "No storage device chosen, scores will not be loaded or saved."
                | Some _ ->
                    ()

                let gameplay = new GameplayComponent.Component(this, controlDevice)
                this.Components.Add(gameplay)
                
                let! score = Async.AwaitEvent(gameplay.GameOver)
                gameplay.Dispose()
                this.Components.Remove(gameplay) |> ignore

                match maybeDevice with
                | None ->
                    ()
                | Some device ->
                    return! afterGamePlay controlDevice device score
            }

        // When the start button is pressed on a controller, go to the next step in the GUI's flow.
        pressStart.StartPressed.Add(fun controlDevice ->
            // We won't be needing the "press start" again, let's dispose of its resources.
            pressStart.Dispose()
            this.Components.Remove(pressStart) |> ignore

            Async.Start (mainTask controlDevice)
        )

        base.Initialize()

    override this.Draw(gt) =
        base.GraphicsDevice.Clear(Color.White)
        base.Draw(gt)