namespace FSharpDeepDives

open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

#if INTERACTIVE
open FSharpDeepDives
#endif

module Registry = 
    
    let mutable private agents = Map.empty<string, AgentRef list>

    let register (ref:AgentRef<'a>) =
        match Map.tryFind ref.Id agents with
        | Some(refs) -> 
            agents <- Map.add ref.Id ((ref :> AgentRef) :: refs) agents
        | None ->
            agents <- Map.add ref.Id [ref :> AgentRef] agents
        ref

    let resolveOne id = 
        match Map.find id agents with
        | [h] -> h
        | _ -> failwithf "Found multiple agents (%s)" id
        
    let resolve id = 
        Map.find id agents

module Agent = 
           
    let start (ref:AgentRef<'a>) =
        ref.Start()
        ref

    let spawn ref = 
        Registry.register ref
        |> start

    let ref (ref:AgentRef<'a>) = ref :> AgentRef

    let post (refs:#seq<AgentRef>) (msg:'a) = 
        refs |> Seq.iter (fun r -> (r :?> AgentRef<'a>).Post(msg))

    let postAndTryAsyncReply (refs:#seq<AgentRef>) msg =
        refs 
        |> Seq.map (fun r -> (r :?> AgentRef<'a>).PostAndTryAsyncReply(msg))
        |> Async.Parallel

    let postAndAsyncReply (refs:#seq<AgentRef>) msg =
        async {
            let! responses = postAndTryAsyncReply refs msg 
            return responses |> Seq.choose id
        }

    let postAndReply (refs:#seq<AgentRef>) msg =
        postAndAsyncReply refs msg |> Async.RunSynchronously

    let resolve id = Registry.resolve id

    type Replyable<'a, 'b> = | Reply of 'a * IAsyncReplyChannel<'b>
    
    let bounded name limit comp = 
        BoundedAgent<_>(name, limit, fun (ref:AgentRef<_>) -> 
            let rec loop (ref:AgentRef<_>) = 
                async {
                    let! Reply(msg, reply) = ref.Receive()
                    let! result = comp msg |> Async.Catch
                    do reply.Reply(result)
                    return! loop ref
                }
            loop ref) |> spawn
    
    let pipelined (agent:AgentRef<_>) previous =
        agent.Id, async { 
            let! result = agent.PostAndTryAsyncReply(fun rc -> Reply(previous,rc)) 
            match result with
            | Some(result) ->
                match result with
                | Choice1Of2(result) -> return result 
                | Choice2Of2(err) -> return raise(err)
            | None -> return failwithf "Stage timed out %s: failed" agent.Id
        }