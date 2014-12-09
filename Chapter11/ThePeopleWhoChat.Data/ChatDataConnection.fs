namespace ThePeopleWhoChat.Data

    open System
    open System.IO
    open System.Linq
    open System.Collections.Generic
    open Raven
    open Raven.Client
    open Raven.Client.Document

    open ThePeopleWhoChat.Core

    type ChatDataConnection(dbPath:string) =

        let docStore = new DocumentStore()

        let sessionCache = new List<LoginSession>()

        do
            docStore.Url <- dbPath
            docStore.Initialize() |> ignore

        member this.sessionWrapper<'a> (token:string) (isPriv:bool) (saveAfter:bool) (f:(IDocumentSession * LoginSession) -> 'a) =
            use session = docStore.OpenSession()
            if sessionCache.Any(fun ls -> ls.token = token) then
                let ls = sessionCache.Where(fun ls -> ls.token = token).First()
                if not(isPriv) || ls.user.isAdmin then
                    let result = f(session,ls)
                    ls.lastTouch <- DateTime.Now
                    if saveAfter then session.SaveChanges()
                    result
                else failwith "Not authorized"
            else
                failwith "Not logged in"
        member this.IsDbEmpty() =
            use session = docStore.OpenSession()
            session.Query<User>().Count() = 0
        member this.InitRootUser(password:string) =
            use session = docStore.OpenSession()
            let user = { Id = null; name = "root"; passwordHash = PasswordHash.GenerateHashedPassword(password); fullName = "System Account"; isAdmin = true }
            session.Store(user)
            session.SaveChanges()
        member this.DeleteAll() =
            use session = docStore.OpenSession()
            let delete x = session.Delete(x)
            session.Query<Message>() |> Seq.iter(delete)
            session.Query<Room>() |> Seq.iter(delete)
            session.Query<User>() |> Seq.iter(delete)
//            session.Query("Raven/Hilo/message") |> Seq.iter(delete)
//            session.Query("Raven/Hilo/room") |> Seq.iter(delete)
//            session.Query("Raven/Hilo/user") |> Seq.iter(delete)
            session.SaveChanges()

        interface IChatServiceClient with
            member this.Login(username:string,password:string) =
                use session = docStore.OpenSession()
                let user = session.Query<User>().Where(fun (u:User) -> u.name = username).FirstOrDefault()
                match box user with
                | null -> failwith "Login failed"
                | _ ->
                    if PasswordHash.VerifyPassword(password,user.passwordHash) then
                        if sessionCache.Any(fun ls -> ls.user = user) then
                            let ls = sessionCache.Where(fun ls -> ls.user = user).First()
                            ls.lastTouch <- DateTime.Now
                            ls.token
                        else
                            let ls = {  token = Guid.NewGuid().ToString(); 
                                        userId = (session.Advanced.GetDocumentId(user)); 
                                        user = user; roomId = None; lastTouch = DateTime.Now }
                            sessionCache.Add(ls)
                            ls.token
                    else
                        failwith "Login failed"

            member this.Logout(token:string) =
                if sessionCache.Any(fun ls -> ls.token = token) then
                    sessionCache.Remove( sessionCache.Where(fun ls -> ls.token = token).First() ) |> ignore
                else failwith "not logged in"

            member this.ListSessions(token:string) =
                this.sessionWrapper token true false (fun ls ->
                    sessionCache |> Array.ofSeq
                    )
                    
            member this.AddUser(token:string, user:User) =
                this.sessionWrapper token true true (fun (session,_) ->
                    let exists = session.Query<User>().Any(fun (u:User) -> u.name = user.name)
                    if exists then failwith (sprintf "duplicate user: %s" user.name)
                    else session.Store(user)
                    session.Advanced.GetDocumentId(user)
                    )

            member this.RemoveUser(token:string, userId:string) =
                this.sessionWrapper token true true (fun (session,_) ->
                    let user = session.Load<User>(userId)
                    session.Delete(user)
                    ) 

            member this.ListUsers(token:string) =
                this.sessionWrapper token true false (fun (session,ls) ->
                    session.Query<User>() |> Array.ofSeq
                    )

            member this.AddRoom(token:string, room:Room) =
                this.sessionWrapper token true true (fun (session,_) ->
                    let exists = session.Query<Room>().Any(fun (r:Room) -> r.name = room.name)
                    if exists then failwith (sprintf "duplicate room: %s" room.name)
                    else session.Store(room)
                    session.Advanced.GetDocumentId(room)
                    )

            member this.RemoveRoom(token:string, roomId:string) =
                this.sessionWrapper token true true (fun (session,_) ->
                    let room = session.Load<Room>(roomId)
                    session.Delete(room)
                    )

            member this.ListRooms(token:string) =
                this.sessionWrapper token false false (fun (session,ls) ->
                    session.Query<Room>() |> Array.ofSeq
                    )

            member this.EnterRoom(token:string, roomName:string) =
                this.sessionWrapper token false false (fun (session,us) ->
                    let room = session.Query<Room>().FirstOrDefault(fun (r:Room) -> r.name = roomName)
                    match box room with
                    | null -> failwith (sprintf "Unknown room: %s" roomName)
                    | _ -> us.roomId <- Some room.Id
                )

            member this.LeaveRoom(token:string) =
                this.sessionWrapper token false false (fun (session,us) ->
                    us.roomId <- None
                )

            member this.GetMessages(token:string, from:DateTime) =
                this.sessionWrapper token false false (fun (session,us) ->
                    match us.roomId with
                    | Some roomId ->
                        let millisecondCompare msg =
                            (msg.timestamp - from).TotalMilliseconds > 1.0
                        let messages = session.Query<Message>().Where(fun (m:Message) -> m.roomId = roomId && m.timestamp > from)
                                            |> Seq.filter(millisecondCompare) |> Array.ofSeq
                        messages
                    | None -> failwith "not in a room"
                )

            member this.PostMessage(token:string, message:string) =
                this.sessionWrapper token false true (fun (session,us) ->
                    match us.roomId with
                    | Some roomId ->
                        let msg = { Id = null; roomId = roomId; timestamp = DateTime.Now.ToLocalTime(); tickCount = Environment.TickCount; userName = us.user.name; 
                                    rawMessage = message; html = MessageParser.Parse(message) }
                        session.Store(msg)
                    | None -> failwith "not in a room"
                )