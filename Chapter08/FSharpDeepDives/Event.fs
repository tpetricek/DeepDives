namespace FSharpDeepDives

open System

#if INTERACTIVE
open FSharpDeepDives
#endif

module Event = 
    
    let create eventType payload metaData =
        {
            Id = Guid.NewGuid()
            EventType = eventType
            Payload = Some(payload)
            Timestamp = DateTimeOffset.UtcNow
            Error = None
            MetaData = dict metaData
        }

    let error eventType (error:exn) metaData = 
        {
            Id = Guid.NewGuid()
            EventType = eventType
            Payload = Unchecked.defaultof<_>
            Timestamp = DateTimeOffset.UtcNow
            Error = Some(box error)
            MetaData = dict metaData
        }

    let (|Success|Empty|Error|) (event:Event<_,_>) = 
        match event.Payload with
        | Some(payload) -> Success(payload)
        | None -> 
            match event.Error with
            | Some(err) -> Error(err)
            | None -> Empty


