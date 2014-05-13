#load "Includes.fsx"

open System
open System.IO
open Newtonsoft.Json 
open FSharpDeepDives
open FSharpDeepDives.Async
open FSharpDeepDives.Etl
open Microsoft.FSharp.Control

//Listing 8.4.1 non wrapped non async
//let etl extractf transformf loadf = 
//    extractf()
//    |> transformf
//    |> loadf

//Listing 8.4.2
let readFile path = 
    (fun () -> File.ReadAllText(path))

let writeJson path input = 
    (path, JsonConvert.SerializeObject(input))
    |> File.WriteAllText

let split (char:string) (input:string) = 
    input.Split([|char|], StringSplitOptions.RemoveEmptyEntries)

let parseCsv (input:string) = 
    split Environment.NewLine input |> Array.map (split ",")


//Lisiting 8.4.3 wrapped non async
let etlNonAsync extractf transformf loadf =
    let stage name success input = 
        match input with
        | Choice1Of2(output) -> 
            try Choice1Of2 <| success output with e -> Choice2Of2 (name,e)
        | Choice2Of2(err) -> Choice2Of2 err

    stage "extract" extractf (Choice1Of2 ())
    |> stage "transform" transformf
    |> stage "load" loadf

let result = 
   etlNonAsync
        (readFile (__SOURCE_DIRECTORY__ + "\data_!.csv"))
        parseCsv 
        (writeJson (__SOURCE_DIRECTORY__ + "\data.json"))

let returnResult input = async { return Choice1Of2 input }

let stage name f rest = 
        async {
                let! f = f |> Async.Catch
                match f with
                | Choice1Of2 r -> 
                    let! result = rest r
                    return result
                | Choice2Of2 e -> return Choice2Of2 (name,e)
        }


let etlAsyncNoSugar extractf transformf loadf =
    stage "extract" extractf 
        (fun extracted ->
             stage "transform" (transformf extracted)
                  (fun transformed -> stage "load" (loadf transformed) 
                                           (fun result -> returnResult result))
        )

let readFileAsync path = 
    async {
        use fs = File.OpenRead(path)
        use sr = new StreamReader(fs)
        return! sr.ReadToEndAsync() |> Async.AwaitTask
    }

let parseCsvAsync (input:string) = 
    async {
            return split Environment.NewLine input |> Array.map (split ",")
    }

let writeJsonAsync path input = 
    async {
        let! serialised = JsonConvert.SerializeObjectAsync(input) |> Async.AwaitTask
        use fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None)
        use sw = new StreamWriter(fs)
        return! sw.WriteAsync(serialised) |> Async.AwaitIAsyncResult |> Async.Ignore
    }

let resultAsyncNoSugar = 
    etlAsyncNoSugar 
        (readFileAsync (__SOURCE_DIRECTORY__ + "\data.csv"))
        parseCsvAsync 
        (writeJsonAsync (__SOURCE_DIRECTORY__ + "\data.json"))
    |> Async.RunSynchronously


let etlAsync (extractf:Async<'a>) (transformf:'a -> Async<'b>) (loadf:'b -> Async<'c>) =
    etl { 
         let! extracted = "extract",extractf
         let! transformed = "transform",transformf extracted
         let! result = "load",loadf transformed
         return result
    }

let resultAsync = 
    etlAsync 
        (readFileAsync (__SOURCE_DIRECTORY__ + "\data.csv"))
        parseCsvAsync 
        (writeJsonAsync (__SOURCE_DIRECTORY__ + "\data.json"))
    |> Async.RunSynchronously

//Lisiting 8.4.7
let parseCSVAndSaveAsJson sourcePath sinkPath = 
    etl {
        let! extracted = "extractCsv", (readFileAsync sourcePath)
        let! parsedCsv = "parseCSV", (parseCsvAsync extracted)
        do! "savingJson", (writeJsonAsync sinkPath parsedCsv)
    } |> Etl.toAsync id (printfn "Error Stage: %s - %A") 
    
let writeOutput (path,input) = 
    async {
        let! serialised = JsonConvert.SerializeObjectAsync(input) |> Async.AwaitTask
        use fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None)
        use sw = new StreamWriter(fs)
        return! sw.WriteAsync(serialised) |> Async.AwaitIAsyncResult |> Async.Ignore
    }
   
let fileExtractor = 
    Agent.bounded "extractCsv" 5 readFileAsync

let parser = 
    Agent.bounded "parseCsv" 5 parseCsvAsync

let saveFile =
    Agent.bounded "saveFile" 5 writeOutput

let agentWorker sourcePath sinkPath = 
    etl {
        let! extracted = Agent.pipelined fileExtractor sourcePath
        let! parsedCsv = Agent.pipelined parser extracted
        do! Agent.pipelined saveFile (sinkPath,parsedCsv)
    } |> Etl.toAsync id (printfn "Error Stage: %s - %A") 

agentWorker @"D:\Appdev\fsharpdeepdives\FSharpDeepDives\data.csv" "D:\workerresult.json" |> Async.RunSynchronously

Schedule.toObservable "0 0/1 * 1/1 * ? *"
|> Observable.add (fun dt -> 
                        printfn "Running CSV converter"
                        parseCSVAndSaveAsJson (__SOURCE_DIRECTORY__ + "\data.csv") (__SOURCE_DIRECTORY__ + "\data.json") |> Async.RunSynchronously)