module SharpVille.Server.DAL

open System
open System.Collections.Generic
open SharpVille.Model

type IStateRepository =
    /// Retrieves a player's game state
    abstract member Get     : PlayerId  -> State option

    /// Saves a player's state
    abstract member Put     : State     -> unit

type ISessionStore =
    /// Retrieves a player's session
    abstract member Get     : SessionId -> Session option

    /// Saves a player's session
    abstract member Put     : Session   -> unit

type InMemoryStateRepo () =
    let states = new Dictionary<PlayerId, State>()

    interface IStateRepository with
        member this.Get(playerId) = 
            match states.TryGetValue playerId with
            | true, x -> Some x
            | _       -> None
        member this.Put(state)  = states.[state.PlayerId] <- state

type InMemorySessionStore () =
    let sessions = new Dictionary<SessionId, Session>()

    interface ISessionStore with
        member this.Get(sessionId) = 
            match sessions.TryGetValue sessionId with
            | true, x -> Some x
            | _       -> None
        member this.Put(session)  = sessions.[session.Id] <- session