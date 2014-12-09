open ThePeopleWhoChat.Core
open ThePeopleWhoChat.Data
open System
open System.IO
open System.Net
open System.Configuration
open System.Text
open System.Threading

type Usage =
    static member General = "Invalid command. Type 'help' for further information."
    static member Connect = "connect (dev|live|<url>)"
    static member ConnectDb = "connectdb [dbUrl]"
    static member DeleteAll = "deleteall [dbUrl]"
    static member Login = "login <username>"
    static member Logout = "logout"
    static member ListSessions = "listsessions"
    static member AddUser = "adduser <username> <password> <fullName> <isAdmin>"
    static member RemoveUser = "removeuser <userId>"
    static member ListUsers = "listusers"    
    static member AddRoom = "addroom <name> <description>"
    static member RemoveRoom = "removeroom <roomId>"
    static member ListRooms = "listrooms"
    static member Enter = "enter <roomId>"
    static member Leave = "leave"
    static member GetMessages = "getm (tail | [from:dateTime])"
    static member PostMessage = "post [message]"
    static member Help() =
        let msgs = [| Usage.Connect; Usage.ConnectDb; Usage.DeleteAll; Usage.Login; Usage.Logout; Usage.ListSessions; Usage.AddUser; 
                      Usage.RemoveUser; Usage.ListUsers; Usage.AddRoom; Usage.RemoveRoom; Usage.ListRooms; 
                      Usage.Enter; Usage.Leave; Usage.GetMessages; Usage.PostMessage |]
        Console.WriteLine("Usage:")
        msgs |> Seq.iter (fun s -> Console.WriteLine("  {0}",s))

let getPassword(prompt:string) =
    let pw = new StringBuilder()
    Console.Write(prompt)
    let mutable inprogress = true
    while inprogress do
        let key = Console.ReadKey(true)
        match key.Key with
        | ConsoleKey.Backspace ->
            if pw.Length > 0 then
                pw.Length <- pw.Length - 1
                Console.Write("\b \b")
        | ConsoleKey.Enter -> inprogress <- false
        | _ ->
            pw.Append(key.KeyChar) |> ignore
            Console.Write("*")
    Console.WriteLine()
    pw.ToString()

let checkAndCreateRoot(db:ChatDataConnection) =
    if db.IsDbEmpty() then
        Console.WriteLine("Database is empty. Creating user 'root'")
        let rootPw = getPassword("Enter root password: ")
        db.InitRootUser(rootPw)

let getDbUrl(usage) = function
    | [] -> ConfigurationManager.AppSettings.[Consts.DbUrlSettingKey]
    | x::[] -> x
    | _ -> raise (System.InvalidProgramException(usage))

let getServiceUrl(usage) = function
    | str::[] when String.Compare(str,"dev",true)=0 -> ConfigurationManager.AppSettings.[Consts.DevUrlSettingKey]
    | str::[] when String.Compare(str,"live",true)=0 -> ConfigurationManager.AppSettings.[Consts.LiveUrlSettingKey]
    | url::[] -> url
    | _ -> raise (System.InvalidProgramException(usage))

let serverNotRunning() =
    let s = StringBuilder("Failed to connect to the RavenDb server.\r\n")
    s.Append("Run RavenDb/Server/Raven.Server.exe prior to starting this shell") |> ignore
    s.ToString() |> failwith

let connectDb(args) =
    let dbUrl = getDbUrl(Usage.ConnectDb)(args)
    try
        let rawDb = ChatDataConnection(dbUrl)
        Console.WriteLine("Connecting directly to database {0}",dbUrl)
        checkAndCreateRoot(rawDb)
        Console.WriteLine("Connected")
        rawDb :> IChatServiceClient
    with
    | :? WebException -> serverNotRunning()

let connectUrl(args) =
    let svcUrl = getServiceUrl(Usage.Connect)(args)
    let svc = ServiceClient(svcUrl)
    Console.WriteLine("Connected to server {0}",svcUrl)
    svc :> IChatServiceClient

let deleteall(args) =
    let dbUrl = getDbUrl(Usage.DeleteAll)(args)
    try
        Console.Write("Sure about that? (Y/N)")
        let c = Console.ReadKey().KeyChar
        Console.WriteLine()
        match c with
        | 'y' | 'Y' ->
            let rawDb = ChatDataConnection(dbUrl)
            rawDb.DeleteAll()
            Console.WriteLine("All data deleted. Reconnect to create 'root' user")
        | _ -> Console.WriteLine("Aborted")
    with
    | :? WebException -> serverNotRunning()

type commandAction = string * IChatServiceClient * string list -> unit
type commandFunc<'T> = string * IChatServiceClient * string list -> 'T
          
let login(token,svc:IChatServiceClient,args) =
    let username = 
        match args with
        | x::[] -> x
        | _ -> raise (System.InvalidProgramException(Usage.Login))
    let password = getPassword("Password: ")
    let token = svc.Login(username, password)
    Console.WriteLine("Logged in. Session token = {0}",token)
    token

let logout(token,svc:IChatServiceClient,args) =
    match args with
    | [] ->
        svc.Logout(token)
        Console.WriteLine("Logged out")
    | _ -> raise (System.InvalidProgramException(Usage.Logout))
          
let listsessions(token,svc:IChatServiceClient,args) =
    match args with
    | [] ->
        let sessions = svc.ListSessions(token)
        Console.WriteLine("User sessions:")
        for us in sessions do
            Console.WriteLine("    user = {0} ({1}), room = {2}, lastTouch = {3}", us.userId, us.user.name, us.roomId, us.lastTouch)
    | _ -> raise (System.InvalidProgramException(Usage.ListSessions))

let adduser(token,svc:IChatServiceClient,args) =
    match args with
    | name::password::fullName::isAdminStr::[] ->
        let isAdminValid,isAdmin = bool.TryParse(isAdminStr)
        let user = { Id = null; name = name; passwordHash = PasswordHash.GenerateHashedPassword(password); fullName = fullName; isAdmin = if isAdminValid then isAdmin else false}
        let userId = svc.AddUser(token,user)
        Console.WriteLine("Created new user: {0} with id: {1}", name, userId)
    | _ -> raise (System.InvalidProgramException(Usage.AddUser))

let removeuser(token,svc:IChatServiceClient,args) =
    match args with
    | userId::[] ->
        svc.RemoveUser(token,userId)
        Console.WriteLine("User with id: {0} removed", userId)
    | _ -> raise (System.InvalidProgramException(Usage.RemoveUser))

let listusers(token,svc:IChatServiceClient,args) =
    match args with
    | [] ->
        let users = svc.ListUsers(token)
        Console.WriteLine("Full user list:")
        for user in users do
            Console.WriteLine("    id: {0}, name: {1}, pwhash: {2}, fullname: {3}, isAdmin: {4}", user.Id, user.name, user.passwordHash, user.fullName, user.isAdmin)
    | _ -> raise (System.InvalidProgramException(Usage.ListUsers))            
      
let addroom(token,svc:IChatServiceClient,args) =
    match args with
    | name::description::[] ->
        let room = { Id = null; name = name; description = description }
        let roomId = svc.AddRoom(token,room)
        Console.WriteLine("Created new room: {0} with id: {1}", name, roomId)
    | _ -> raise (System.InvalidProgramException(Usage.AddRoom))

let removeroom(token,svc:IChatServiceClient,args) =
    match args with
    | roomId::[] ->
        svc.RemoveRoom(token,roomId)
        Console.WriteLine("Room with id: {0} removed", roomId)
    | _ -> raise (System.InvalidProgramException(Usage.RemoveRoom))

let listrooms(token,svc:IChatServiceClient,args) =
    match args with
    | [] ->
        let rooms = svc.ListRooms(token)
        Console.WriteLine("Full room list:")
        for room in rooms do
            Console.WriteLine("    id: {0}, name: {1}, description: {2}", room.Id, room.name, room.description)
    | _ -> raise (System.InvalidProgramException(Usage.ListRooms))   
                          
let mutable lastMessage = DateTime.MinValue

let enterroom(token,svc:IChatServiceClient,args) =
    match args with
    | roomId::[] ->
        svc.EnterRoom(token,roomId)
        lastMessage <- DateTime.MinValue
        Console.WriteLine("Entered room: {0}", roomId)
    | _ -> raise (System.InvalidProgramException(Usage.Enter))
    
let leaveroom(token,svc:IChatServiceClient,args) =
    match args with
    | [] ->
        svc.LeaveRoom(token)
        Console.WriteLine("Left the room")
    | _ -> raise (System.InvalidProgramException(Usage.Leave))          
    
let getmessages(token,svc:IChatServiceClient,args) =
    let (fromTime, tailmode) =
        match args with
        | "tail"::[] -> Console.WriteLine("tail mode. press Q to end.")
                        lastMessage,true
        | dateStr::ticksStr::[] ->
            let valid,date = DateTime.TryParse(dateStr)
            if valid then date,false
            else raise (System.InvalidProgramException(Usage.GetMessages))
        | [] -> lastMessage,false
        | _ -> raise (System.InvalidProgramException(Usage.GetMessages))
    let rec innergetmessages(fromTime) =
        let messages = svc.GetMessages(token, fromTime)
        for m in messages do
            Console.WriteLine("  {0:HHmm} {1}: {2}", m.timestamp, m.userName, m.rawMessage)
            lastMessage <- m.timestamp
        if tailmode then
            Thread.Sleep(1000)
            if Console.KeyAvailable then
                let key = Console.ReadKey().KeyChar
                if key = 'q' || key = 'Q' then Console.WriteLine()
                else innergetmessages(lastMessage)
            else innergetmessages(lastMessage)
    innergetmessages(fromTime)

            
                      
let postmessage(token,svc:IChatServiceClient,args) =
    match args with
    | msg::[] -> svc.PostMessage(token,msg)
    | [] ->
        Console.Write("Message: ")
        let msg = Console.ReadLine()
        svc.PostMessage(token,msg)
    | _ -> raise (System.InvalidProgramException(Usage.PostMessage))

[<EntryPoint>]
let main args =

    let (dataConnection:IChatServiceClient option ref) = ref None

    let executeCommand(token:string,args:string list,f:commandAction) =
        if (!dataConnection).IsNone then 
            failwith "Not connected"
        else 
            f(token,((!dataConnection).Value),args)
    let executeFunc(token:string,args:string list,f:commandFunc<'T>) =
        if (!dataConnection).IsNone then 
            failwith "Not connected"
        else 
            f(token,((!dataConnection).Value),args)

    let mutable token = ""

    let rec fileReader() = File.ReadAllLines(args.[0]) |> Seq.ofArray

    let rec consoleReader() = seq {
        Console.Write("> ")
        let line = Console.ReadLine()
        match line with
        | "quit"
        | "exit" -> Console.WriteLine("byebye")
        | x -> yield x
               yield! consoleReader()
        }

    let reader = if args.Length = 1 then fileReader() else consoleReader()

    for line in reader do

        try
            match (line |> LineParser.SpaceSeperatedParser.Parse |> List.ofSeq) with
            | "help"::args -> Usage.Help()
            | "connectdb"::args ->
                let db = connectDb(args)
                dataConnection := Some db
            | "connect"::args ->
                let svc = connectUrl(args)
                dataConnection := Some svc
            | "deleteall"::args ->
                deleteall(args)
                dataConnection := None
                Console.WriteLine("Disconnected")
            | "login"::args ->
                token <- executeFunc(token,args,login)
            | "logout"::args ->
                executeCommand(token,args,logout)
            | "listsessions"::args ->
                executeCommand(token,args,listsessions)
            | "adduser"::args ->
                executeCommand(token,args,adduser)
            | "removeuser"::args ->
                executeCommand(token,args,removeuser)
            | "listusers"::args ->
                executeCommand(token,args,listusers)
            | "addroom"::args ->
                executeCommand(token,args,addroom)
            | "removeroom"::args ->
                executeCommand(token,args,removeroom)
            | "listrooms"::args ->
                executeCommand(token,args,listrooms)
            | "enter"::args ->
                executeCommand(token,args,enterroom)
            | "leave"::args ->
                executeCommand(token,args,leaveroom)
            | "getm"::args ->
                executeCommand(token,args,getmessages)
            | "post"::args ->
                executeCommand(token,args,postmessage)
            | _ -> Console.WriteLine(Usage.General)
        with
        | :? System.InvalidProgramException as ex -> Console.WriteLine("Usage: {0}",ex.Message)
        | Failure(e) -> Console.WriteLine(e)

    0

