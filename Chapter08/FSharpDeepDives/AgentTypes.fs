namespace FSharpDeepDives

open System
open Microsoft.FSharp.Control
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

#if INTERACTIVE
open FSharpDeepDives
#endif

[<AutoOpen>]
module AgentTypes =
    
    type IAsyncReplyChannel<'a> =
        abstract Reply : 'a -> unit

    type MailboxReplyChannel<'a>(asyncReplyChannel:AsyncReplyChannel<'a>) =
        interface IAsyncReplyChannel<'a> with
            member x.Reply(msg) = asyncReplyChannel.Reply(msg)

    type ReplyChannel<'a>() = 
         let tcs = new TaskCompletionSource<'a>()
    
         member x.WaitResult =
            async {
                return! tcs.Task |> Async.AwaitTask
            }

         interface IAsyncReplyChannel<'a> with
             member x.Reply(msg) = 
                tcs.SetResult(msg)

    [<AbstractClass>]
    type AgentRef(id:string) =
        member val Id = id with get, set
        abstract Start : unit -> unit


    [<AbstractClass>]
    type AgentRef<'a>(id:string) =
        inherit AgentRef(id)
        abstract Receive : unit -> Async<'a>
        abstract Post : 'a -> unit
        abstract PostAndTryAsyncReply : (IAsyncReplyChannel<'b> -> 'a) -> Async<'b option>

    type Agent<'a>(id:string, comp, ?token) = 
        inherit AgentRef<'a>(id)
        let mutable agent = Unchecked.defaultof<MailboxProcessor<'a>>
        
        override x.Post(msg:'a) = agent.Post(msg)
        override x.PostAndTryAsyncReply(builder) = agent.PostAndTryAsyncReply(fun rc -> builder(new MailboxReplyChannel<_>(rc)))

        override x.Receive() = agent.Receive()
        override x.Start() = 
            agent <- MailboxProcessor.Start((fun inbox -> comp (x :> AgentRef<_>)), ?cancellationToken = token)

    type BoundedAgent<'a>(id:String, limit:int, comp, ?token) = 
        inherit AgentRef<'a>(id)
        let cts = defaultArg token Async.DefaultCancellationToken

        let bc = new BlockingCollection<'a>(limit - 1)

        override x.Post(msg) = bc.Add(msg)

        override x.PostAndTryAsyncReply(builder) = 
            async { 
                let rc = new ReplyChannel<_>()
                do bc.Add(builder(rc))
                let! result = rc.WaitResult
                return Some result
            }

        override x.Receive() = async { return bc.Take(cts) }

        override x.Start() = 
            Async.Start(comp (x :> AgentRef<_>), cts)


        
