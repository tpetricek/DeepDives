namespace ThePeopleWhoChat.Core

    open System

    type IChatServiceClient =
        abstract member Login: string * string -> string
        abstract member Logout: string -> unit
        abstract member ListSessions: string -> LoginSession array

        abstract member AddUser: string * User -> string
        abstract member RemoveUser: string * string -> unit
        abstract member ListUsers: string -> User array

        abstract member AddRoom: string * Room -> string
        abstract member RemoveRoom: string * string -> unit
        abstract member ListRooms: string -> Room array

        abstract member EnterRoom: string * string -> unit
        abstract member LeaveRoom: string -> unit
        abstract member GetMessages: string * DateTime -> Message array
        abstract member PostMessage: string * string -> unit