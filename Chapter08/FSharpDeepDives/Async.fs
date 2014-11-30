namespace FSharpDeepDives

module Async =
    open System
    open System.Threading.Tasks
    open System
    open System.Diagnostics

    let rec retry count interval (isRetryException:System.Exception->bool) (work:Async<'T>) = 
        async { 
            try 
                let! result = work
                return Choice1Of2 result
            with e ->
                if isRetryException e && count > 0 then
                    do! Async.Sleep interval  
                    return! retry (count - 1) interval isRetryException work
                else 
                    return Choice2Of2 e
        }

    let sleep intervalMilliseconds (stopwatch:Stopwatch) = async {
            let elapsedMilliseconds = int stopwatch.ElapsedMilliseconds
            if elapsedMilliseconds < intervalMilliseconds then
                do! Async.Sleep(intervalMilliseconds - elapsedMilliseconds)
        }
                        
    let poll intervalMilliseconds initialValue errorF workF =
        let stopwatch = Stopwatch()
        let rec loop value =
            async {
                stopwatch.Restart()
                let! result = workF value |> Async.Catch
                stopwatch.Stop()
                match result with
                | Choice1Of2 nextValue ->
                    do! sleep intervalMilliseconds stopwatch
                    return! loop nextValue
                | Choice2Of2 e -> 
                    do errorF e
                    do! sleep intervalMilliseconds stopwatch
                    return! loop value
            }
        loop initialValue
        
    let toTask (async : Async<_>) = 
        Task.Factory.StartNew(fun _ -> Async.RunSynchronously(async))

    let toActionTask (async : Async<_>) = 
        Task.Factory.StartNew(new Action(fun () -> Async.RunSynchronously(async) |> ignore))