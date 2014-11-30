module ScoreComponent

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics

// Type of a board of scores and the new score to insert into the board.
type ScoreBoard =
    { Scores : (string * int) [] // The names and scores, ordered by decreasing score.
      NewScore : string * int } // The new name and score to insert into the board.

// States of the state machine
type RenderingState =
    | DoneAtBottom // The new score is too low and is rendered at the bottom
    | DoneInserted of int // The new score has reached its position on the board
    | Busy of int * float32 // The new score is rising to its position on the board

// Computes the initial state of the board
let initialize board =
    let (_, newScore) = board.NewScore
    match board.Scores, Array.tryFindIndex (fun (_, s) -> s <= newScore) board.Scores with
    | [||], _ ->
        DoneInserted 0 // Special case: no scores in the table yet
    | _, None ->
        DoneAtBottom // New score is lower than any score on the board
    | _, Some idx ->
        Busy(board.Scores.Length, 0.0f) // Start the animation by displaying score 0 at the bottom of the board

// Updates the state machine
let update board scoreIncrement dt state =
    match state with
    | DoneInserted _
    | DoneAtBottom -> state // DoneInserted and DoneAtBottom are final states
    | Busy(idx, score) -> // Busy state can lead to Busy with updated values or to DoneInserted if the displayed score reaches its final value and position.
        let (_, target) = board.NewScore
        let score2 = min (score + dt * scoreIncrement) (float32 target) // New value of score to be displayed
        match Array.tryFindIndex (fun (_, s) -> float32 s <= score2) board.Scores with // Find the position on the score board
        | None ->
            Busy(idx, score2) // Still at the bottom
        | Some idx ->
            if score2 >= (float32 target) then
                DoneInserted idx // The displayed value has reached the final value
            else
                Busy(idx, score2) // The animation continues, possibly with an updated position on the board

// Renders the current frame of the animation
let render (batch : SpriteBatch) (font : SpriteFont) (dev : GraphicsDevice) board state =
    let maxNumLines = 11 // We only show the ten best scores from the board in addition to the new score
    let safe = dev.Viewport.TitleSafeArea // The area of the screen that is garanteed to be visible on any TV (the edges are typically hidden under the TV's casing)
    let spacing = safe.Height / maxNumLines // Vertical space between scores
    let y0 = safe.Top // Vertical position of the best score on the screen
    let x = safe.Left + 50 // Horizontal position of all scores
    
    let format idx name score = sprintf "%2d.%8s...%06d" idx name score // Produces a string showing a rank, a name and a score
    
    let drawString (s : string, y : int, color : Color) =
        batch.DrawString(font, s, Vector2(float32 x, float32 y), color) // Renders a score on screen at a specific position with a the given color

    // Renders a slice of the score board
    let renderSlice (y, idx, first, last) =
        let mutable y = y
        let mutable idx = idx
        for name, score in board.Scores.[first .. last] do
            let s = format idx name score
            drawString(s, y, Color.Black)
            y <- y + spacing
            idx <- idx + 1
        (y, idx)

    try // Make sure batch.End is called even if an exception is thrown
        batch.Begin()
        match state with
        | DoneAtBottom -> // Render the entire score board and the new score at the bottom in red
            let y, idx = renderSlice(y0, 1, 0, board.Scores.Length - 1)
            let name, score = board.NewScore
            let s = format idx name score
            drawString(s, y, Color.Red)
        | DoneInserted pos -> // Render the score board with the new score inserted at its final position
            let y, idx = renderSlice(y0, 1, 0, pos - 1)
            let name, score = board.NewScore
            let s = format idx name score
            drawString(s, y, Color.Green)
            renderSlice(y + spacing, idx + 1, pos, board.Scores.Length - 1) |> ignore
        | Busy(pos, score) -> // Render the score board with the new score inserted at a position matching the animated score value
            let y, idx = renderSlice(y0, 1, 0, pos - 1)
            let name, _ = board.NewScore
            let score = int score
            let s = format idx name score
            drawString(s, y, Color.Green)
            renderSlice(y + spacing, idx + 1, pos, board.Scores.Length - 1) |> ignore
    finally
        batch.End()

// A font and a batch used to render the score board
type Resources =
    { font : SpriteFont
      batch : SpriteBatch
    }

// A C#-friendly drawable game component showing the animated score board
type ScoreComponent(game, content : Content.ContentManager, board) =
    inherit DrawableGameComponent(game)

    let mutable resources = None // Set to None until LoadContent is called by the XNA framework
    let mutable state = initialize board
    let scoreIncrement =
        let animationLength = 5.0f // Duration of the animation, in seconds
        float32 (snd board.NewScore) / animationLength // The increment used for the animated new score value
    
    // Loads the font "font.spritefont" from the content project and create a SpriteBatch for our rendering needs
    override this.LoadContent() =
        let font = content.Load("font")
        resources <-
            Some {
                font = font
                batch = new SpriteBatch(this.GraphicsDevice)
            }

    // If this game component is enabled, updates the state machine
    override this.Update(gt) =
        if this.Enabled then
            let dt = float32 gt.ElapsedGameTime.TotalSeconds
            state <- update board scoreIncrement dt state
        base.Update(gt)

    // Draws the current frame of the animation, if the font was loaded and the sprite batch created
    override this.Draw(gt) =
        let backgroundColor = Color.LightGray
        game.GraphicsDevice.Clear(backgroundColor)
        match resources with
        | Some rsc ->
            render rsc.batch rsc.font this.GraphicsDevice board state
        | None ->
            ()
        base.Draw(gt)

    // Disposes the sprite batch, if it was created
    override this.Dispose(disposeManaged) =
        base.Dispose(disposeManaged)
        match resources with
        | Some { batch = batch } ->
            batch.Dispose()
        | None ->
            ()

let rnd = new System.Random()

let mkTestBoard scores =
    let newScore = 9000 //rnd.Next(10000)
    { Scores = scores
      NewScore = ("Human", newScore) }

(* An alternative implementation using records instead of discriminated unions *)
module RecordBasedImplementation =
    type GameBoardState =
        { board : ScoreBoard
          isAtBottom : bool
          isCompleted : bool
          isBusy : bool
          pos : int
          score : float32
        }

    let newGameBoardState board =
        let defaultState =
            { board = board
              isAtBottom = false
              isCompleted = false
              isBusy = false
              pos = 0
              score = 0.0f }

        let (_, newScore) = board.NewScore
        match Array.tryFindIndex (fun (_, s) -> s <= newScore) board.Scores with
        | None ->
            { defaultState with isAtBottom = true }
        | Some idx ->
            let numScores = board.Scores.Length
            if numScores = 0 then
                { defaultState with isCompleted = true }
            else
                { defaultState with isBusy = true; pos = board.Scores.Length }

    let update scoreIncrement dt state =
        match state with
        | { isCompleted = true }
        | { isAtBottom = true } -> state
        | { isBusy = true } ->
            let (_, target) = state.board.NewScore
            let score2 = min (state.score + dt * scoreIncrement) (float32 target)
            match Array.tryFindIndex (fun (_, s) -> float32 s <= score2) state.board.Scores with
            | None ->
                { state with score = score2 }
            | Some idx ->
                if score2 >= (float32 target) then
                    { state with isBusy = false; isCompleted = true; pos = idx }
                else
                    { state with score = score2 }
        | _ -> failwith "Unexpected state"