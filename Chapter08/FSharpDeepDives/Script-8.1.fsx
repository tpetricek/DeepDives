#load "Includes.fsx"
open System.IO
open System.Net
open Microsoft.FSharp.Control
open FSharpDeepDives.Agent

//Listings

//1.
let doWebRequest (url:string) methd handler = 
      async {
           let request = WebRequest.Create(url, Method = methd)
           use! response = request.AsyncGetResponse()
           return! handler (response.GetResponseStream())
      }

//2. 
let readStreamAsString (stream:Stream) = 
    async {
        use streamReader = new StreamReader(stream)
        return! streamReader.ReadToEndAsync() |> Async.AwaitTask 
    }

//3.
let result  = 
    doWebRequest "http://www.google.com" "GET" readStreamAsString
    |> Async.RunSynchronously

//4.

type Agent<'a> = MailboxProcessor<'a>

type Request = 
    | Get of string * AsyncReplyChannel<string>

let downloadAgent = 
    Agent<_>.Start(fun inbox -> 
        let rec loop (seenUrls : Map<string, string>) = 
            async {
                let! msg = inbox.Receive()
                match msg with
                | Get(url, reply) -> 
                    match seenUrls.TryFind(url) with
                    | Some(result) -> 
                        reply.Reply(result)
                        return! loop seenUrls
                    | None -> 
                        let! downloaded = doWebRequest url "GET" readStreamAsString
                        reply.Reply(result)
                        return! loop (Map.add url downloaded seenUrls)
            }
        loop Map.empty
    )

#load "Includes.fsx"
module DSLExample =
    open FSharpDeepDives
    open FSharpDeepDives.Agent

    let console =
         let agent =
             Agent("console-writer", (fun (agent:AgentRef<string>) ->
                     let rec loop() =
                         async {
                             let! msg = agent.Receive()
                             printfn "%s" msg
                             return! loop()
                         }
                     loop()
             ))
         agent.Start()
         agent
    
    console.Post("Writing through an agent")
    
    //8.2.6
    let registered = 
        let agent =
            Agent("console-writer", (fun (agent:AgentRef<string>) ->
                    let rec loop() =
                        async {
                            let! msg = agent.Receive()
                            printfn "%s" msg
                            return! loop()
                        }
                    loop()
            ))
        agent.Start()
        agent |> Registry.register
    
    //8.2.7
    
    Registry.resolve "console-writer" |> List.iter (fun a -> (a :?> AgentRef<string>).Post("Hello"))
    
    //8.2.9
    let registeredsl = 
        Agent("console-writer", fun (agent:AgentRef<string>) ->
                    let rec loop() =
                        async {
                            let! msg = agent.Receive()
                            printfn "%s" msg
                            return! loop()
                        }
                    loop()
            )
        |> spawn
    
    resolve "console-writer" |> post <| "Writing through an agent"
    
    type Request =
        | Get of string * IAsyncReplyChannel<string>

    let urlReader = 
        Agent("url-reader", fun (agent:AgentRef<Request>) ->
                let rec loop (cache : Map<string, string>) = 
                    async {
                        let! msg = agent.Receive()
                        match msg with
                        | Get(url, reply) -> 
                            match cache.TryFind(url) with
                            | Some(result) -> 
                                reply.Reply(result)
                                return! loop cache
                            | None -> 
                                let! downloaded = doWebRequest url "GET" readStreamAsString
                                reply.Reply(downloaded)
                                return! loop (Map.add url downloaded cache)
                    }
                loop Map.empty) 
        |> spawn
    
    
    resolve "url-reader" |> postAndReply <| (fun rc -> Get("http://www.google.com", rc)) 
    |> Seq.iter (printfn "%A")