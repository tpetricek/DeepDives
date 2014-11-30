module Dialogs

open Microsoft.Xna.Framework.GamerServices

let doWhenGuideNoLongerVisible action =
    let rec work() =
        async {
            while Guide.IsVisible do // Check if the guide is visible
                do! Async.Sleep(500) // If so, wait for half a second. The use of do! and Async ensure that the thread is made available to run other active operations.
            try
                return! action
            with
            // Note: this is needed, as it is possible to get this exception even though Guide.IsVisible returned false.
            | :? GuideAlreadyVisibleException ->
                return! work() // Try again. Notice the use of recursion.
        }
    work()

let showMessageBox title msg buttons focusButton icon =
    let beginFun(callback, arg) =
        Guide.BeginShowMessageBox(title, msg, buttons, focusButton, icon, callback, arg)
    Async.FromBeginEnd(beginFun, Guide.EndShowMessageBox)

let showError msg =
    async {
        let! _ = showMessageBox "Error" msg ["OK"] 0 MessageBoxIcon.Error
        return ()
    }

let showAlert title msg =
    async {
        let! _ = showMessageBox title msg ["OK"] 0 MessageBoxIcon.Alert
        return ()
    }