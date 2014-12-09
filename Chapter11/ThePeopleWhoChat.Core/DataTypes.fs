namespace ThePeopleWhoChat.Core

    open System
    open System.Runtime.Serialization

    type Consts = 
        static member DbUrlSettingKey = "ChatDbUrl"
        static member DevUrlSettingKey = "DevServiceUrl"
        static member LiveUrlSettingKey = "LiveServiceUrl"
        static member TokenHeaderName = "Session-Token"
        static member ErrorHeaderName = "Error-Message"
        static member CacheHeaderName = "Cache-Control"
        static member CacheNoCache = "no-cache"
        static member CacheOneMinute = "public, max-age=60"

    [<DataContract>]
    type Identifier = {
        [<DataMember>] mutable id: string
        }

    [<DataContract>]
    type MessageText = {
        [<DataMember>] mutable message: string
        }

    [<DataContract>]
    type User = {
        [<DataMember>] mutable Id: string;
        [<DataMember>] mutable name: string;
        [<DataMember>] mutable passwordHash: string;
        [<DataMember>] mutable fullName: string;
        [<DataMember>] mutable isAdmin: bool
        }

    [<DataContract>]
    type Room = {
        [<DataMember>] mutable Id: string;
        [<DataMember>] mutable name: string;
        [<DataMember>] mutable description: string
        }

    [<DataContract>]
    type Message = {
        [<DataMember>] mutable Id: string;
        [<DataMember>] mutable roomId: string;
        [<DataMember>] mutable timestamp: DateTime;
        [<DataMember>] mutable tickCount: int;
        [<DataMember>] mutable userName: string;
        [<DataMember>] mutable rawMessage: string;
        [<DataMember>] mutable html: string
        }

    [<DataContract>]
    type LoginDetails = {
        [<DataMember>] mutable username: string;
        [<DataMember>] mutable password: string
        }

    [<DataContract>]
    type LoginSession = {
        [<DataMember>] mutable token: string;
        [<DataMember>] mutable userId: string
        [<DataMember>] mutable user: User;
        [<DataMember>] mutable roomId: string option
        [<DataMember>] mutable lastTouch: DateTime
        }