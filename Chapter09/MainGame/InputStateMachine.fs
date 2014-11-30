module PracticalFSharp.InputStateMachine

type InputStateMachine<'InputState, 'Result> =
    | Active of ('InputState -> InputStateMachine<'InputState, 'Result>)
    | Done of 'Result

/// Wait until a key or button is pressed, compute a return value, then wait until the key or button is released
// Used for selecting the controlling played in the "press start" screen.
// Also used for pausing/resuming during gameplay.
let waitReleased pressed released func =
    let rec waitPressed =
        fun (inputState) ->
            if pressed(inputState) then
                let result = func inputState
                waitReleased result
            else
                waitPressed
        |> Active
        
    and waitReleased result =
        fun (inputState) ->
            if released(inputState) then
                Done result
            else
                waitReleased result
        |> Active

    waitPressed
