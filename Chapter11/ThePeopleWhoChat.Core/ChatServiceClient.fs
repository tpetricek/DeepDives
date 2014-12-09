namespace ThePeopleWhoChat.Core

    open System
    open System.Text
    open System.Net
    open System.IO
    open System.Web
    open System.Runtime.Serialization.Json

    type ServiceClient(url:string) =

        member private x.JsonSerialize<'T>(data:'T) =
            use ms = new MemoryStream()
            let ser = new DataContractJsonSerializer(typeof<'T>)
            ser.WriteObject(ms, data)
            ms.Seek(0L,SeekOrigin.Begin) |> ignore
            use sr = new StreamReader(ms)
            sr.ReadToEnd()

        member private x.makeRequest<'T>(token:string,verb:string,url:string,data:'T) =
            let req = WebRequest.Create(new Uri(url)) :?> HttpWebRequest
            req.Method <- verb
            req.Headers.Add(Consts.TokenHeaderName,token)      
            let json = x.JsonSerialize(data)      
            if not (typeof<'T> = typeof<unit>) then
                let buffer = Encoding.UTF8.GetBytes(json)                
                req.ContentType <- "application/json"
                req.ContentLength <- buffer.LongLength
                use reqSt = req.GetRequestStream()
                reqSt.Write(buffer,0,buffer.Length)
            req
        member private x.WebRequestWrapper<'U>(f:unit->'U) =
            try
                f()
            with
            | :? WebException as e ->
                    let resp = e.Response :?> HttpWebResponse
                    let err = resp.Headers.[Consts.ErrorHeaderName]
                    let msg = match box err with
                                | null -> "Service failure"
                                | _ -> err
                    failwith (sprintf "%s: status = %d (%s)" msg (int32 resp.StatusCode) (resp.StatusCode.ToString()))
        member private x.getResponse<'U>(req:HttpWebRequest) =
            x.WebRequestWrapper(fun () ->
                let res = req.GetResponse() :?> HttpWebResponse
                use resSt = res.GetResponseStream()
                let ser = DataContractJsonSerializer(typeof<'U>)
                ser.ReadObject(resSt) :?> 'U
            )
        member private x.getResponseUnit(req:HttpWebRequest) =
            x.WebRequestWrapper(fun () ->
                let res = req.GetResponse() :?> HttpWebResponse
                ())
        member private x.getData<'U>(token:string,path:string) =
            let fullPath = String.Format("{0}/{1}",url,path)
            let req = x.makeRequest(token,"GET",fullPath,())
            x.getResponse<'U>(req)
        member private x.putDataUnit<'T>(token:string,path:string,data:'T) =
            let fullPath = String.Format("{0}/{1}",url,path)
            let req = x.makeRequest(token,"PUT",fullPath,data)
            x.getResponseUnit(req)
        member private x.putData<'T,'U>(token:string,path:string,data:'T) =
            let fullPath = String.Format("{0}/{1}",url,path)
            let req = x.makeRequest(token,"PUT",fullPath,data)
            x.getResponse<'U>(req)
        member private x.postData<'T,'U>(token:string,path:string,data:'T) =
            let fullPath = String.Format("{0}/{1}",url,path)
            let req = x.makeRequest(token,"POST",fullPath,data)
            x.getResponse<'U>(req)
        member private x.deleteData(token:string,path:string) =
            let fullPath = String.Format("{0}/{1}",url,path)
            let req = x.makeRequest(token,"DELETE",fullPath,())
            x.getResponseUnit(req)

        interface IChatServiceClient with

            member this.Login(username:string,password:string) =
                let logon = {username = username; password = password}
                let req = this.makeRequest("","PUT",String.Format("{0}/token",url),logon)
                let res = this.WebRequestWrapper(fun () ->
                    req.GetResponse() :?> HttpWebResponse
                    )
                res.Headers.[Consts.TokenHeaderName]

            member this.Logout(token:string) =
                this.deleteData(token,String.Format("token/{0}",token))

            member this.ListSessions(token:string) =
                this.getData<LoginSession array>(token,"sessions")

            member this.AddUser(token:string, user:User) =
                this.putData<User,string>(token,"users",user)

            member this.RemoveUser(token:string, userId:string) =
                if userId.StartsWith("users/") then this.deleteData(token,userId)
                else failwith (sprintf "invalid userId: %s" userId)

            member this.ListUsers(token:string) =
                this.getData<User array>(token,"users")

            member this.AddRoom(token:string, room:Room) =
                this.putData<Room,string>(token,"rooms",room)

            member this.RemoveRoom(token:string, roomId:string) =
                if roomId.StartsWith("rooms/") then this.deleteData(token,roomId)
                else failwith (sprintf "invalid roomId: %s" roomId)

            member this.ListRooms(token:string) =
                this.getData<Room array>(token, "rooms")
                
            member this.EnterRoom(token:string, roomId:string) =
                this.putDataUnit<Identifier>(token,"currentroom",{id = roomId})

            member this.LeaveRoom(token:string) =
                this.deleteData(token,"currentroom")

            member this.GetMessages(token:string, from:DateTime) =
                this.getData<Message array>(token, String.Format("messages?after={0:yyyyMMddHHmmssfffffff}",from))

            member this.PostMessage(token:string, message:string) =
                this.putDataUnit<MessageText>(token,"messages",{message = message})