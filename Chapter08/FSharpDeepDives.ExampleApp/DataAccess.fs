namespace FSharpDeepDives.ExampleApp

open System
open System.IO
open FSharpDeepDives
open System.Threading.Tasks

module DataAccess = 
    
    /// <summary>
    /// Stores an event
    /// </summary>
    /// <param name="source">The source of the event</param>
    /// <param name="data">The data to be contained within the event</param>
    let storeEvent source (data:'a) = 
        async {
            let event = Event.create source data [] 
            do Logger.log <| sprintf "Stored event: %A" source
        }

    /// <summary>
    /// Makes an asynchronous HTTP request and returns the result as a string
    /// </summary>
    /// <param name="uri">The URI to make the request to</param>
    /// <param name="query">The query parameters for the query</param>
    let getHttp uri query = 
        async {
            return! FSharp.Data.Http.AsyncRequestString(uri, query, httpMethod="GET")
        }

    /// <summary>
    /// Reads (asynchronuosly) the entire contents of a file as a string 
    /// </summary>
    /// <param name="filePath"></param>
    let getFile filePath = 
        async {
            use fs = File.OpenRead(filePath)
            use sr = new StreamReader(fs)
            return! sr.ReadToEndAsync() |> Async.AwaitTask
        }