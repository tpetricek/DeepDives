namespace ThePeopleWhoChat.Data_
    open Raven.Client
    open Raven.Client.Document
    open ThePeopleWhoChat.Core

    type ChatDataConnection(dbPath:string) =
        let docStore = new DocumentStore(Url = dbPath)
        do docStore.Initialize() |> ignore

        member private this.sessionWrapper<'a> 
                saveAfter (f:IDocumentSession -> 'a) =
            use session = docStore.OpenSession()
            let result = f(session)
            if saveAfter then session.SaveChanges()
            result

        member this.AddUser(user:User) =
            this.sessionWrapper true (fun sess -> sess.Store(user))

        member this.DeleteUser(user:User) =
            this.sessionWrapper true (fun sess -> sess.Delete(user))

        member this.ListUsers() =
            this.sessionWrapper false (fun sess -> 
                sess.Query<User>() |> Array.ofSeq)

        // and so on for rooms and messages...