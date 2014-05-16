namespace FSharpDeepDives

open System
open System.Collections.Generic
open Microsoft.FSharp.Control
open System.Threading

[<AutoOpen>]
module DomainTypes =
            
    type Event<'evntType, 'payload> = {
        Id : Guid
        EventType : 'evntType
        Payload : 'payload option
        Timestamp : DateTimeOffset
        Error : obj option
        MetaData : IDictionary<string, obj> 
    }