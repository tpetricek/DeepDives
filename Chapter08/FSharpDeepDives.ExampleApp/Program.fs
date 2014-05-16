namespace FSharpDeepDives.ExampleApp

open System.Threading
open FSharpDeepDives
open FSharpDeepDives.Agent

module Program = 

    let tokenSource = new CancellationTokenSource()

    ///Create the agents that represent the models
    let models = [
                     Models.total (Some tokenSource.Token)
                     Models.average (Some tokenSource.Token)
                 ] |> List.map (Agent.spawn >> Agent.ref)

    ///Create the triggers that can be used to start the ETL pipleines
    let triggers = [
        Triggers.file "C:\FileDrop\HalfHourFuel"
        Triggers.cron "0 0/15 * 1/1 * ? *"
    ]

    ///Create a simple dispatcher that broadcasts any incoming data to 
    ///all of the models
    let modelDispatcher data = 
        async { 
            do models |> post <| Data(data)
        }

    ///Create the computation parameterised by a trigger
    let computation trigger =
        Elexon.run modelDispatcher trigger
        |> Etl.toAsync id Logger.logStageError
        

    [<EntryPoint>]
    let main(args) = 
        ///Merge the various triggers together into a single observable
        ///Start a computation everytime a trigger is fired.
        triggers
        |> List.reduce Observable.merge
        |> Observable.add (fun trigger -> Async.StartImmediate(computation trigger, tokenSource.Token))

        printfn "System Running: Press Enter to Exit"
        System.Console.ReadLine() |> ignore
        tokenSource.Cancel()

        printfn "System Exiting"
        0
    


