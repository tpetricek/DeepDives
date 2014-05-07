namespace FSharpDeepDives.ExampleApp

module Triggers = 
    
    open System
    open System.IO
    open FSharpDeepDives
    open FSharpDeepDives.ExampleApp

    let private ensurePath path = 
        let di = new DirectoryInfo(path)
        if di.Exists
        then di.FullName
        else di.Create(); di.FullName
    
    /// <summary>
    /// Watches a file path for changed or created events. And extracts the target file.
    /// </summary>
    /// <param name="path">The file path to watch</param>
    let file path = 
        let watcher = new FileSystemWatcher(ensurePath path)
        watcher.EnableRaisingEvents <- true
        watcher.Created
        |> Observable.filter (fun x -> (x.ChangeType = WatcherChangeTypes.Changed) 
                                       || (x.ChangeType = WatcherChangeTypes.Created))
        |> Observable.map (fun x -> File(x.FullPath))
        
    /// <summary>
    /// Creates an observable based on a cron expression. The observable will be fired
    /// every time the cron expression is matched.
    /// </summary>
    /// <param name="cronExp">The cron expression</param>
    let cron cronExp =  
        Schedule.toObservable cronExp
        |> Observable.map (fun _ -> Cron)
