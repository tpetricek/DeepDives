namespace FSharpDeepDives

open System.Collections.Concurrent
module Etl =
    
    let stage name f rest = 
        async {
                let! f = f |> Async.Catch
                match f with
                | Choice1Of2 r -> 
                    let! result = rest r
                    return result
                | Choice2Of2 e -> return Choice2Of2 (name,e)
        }

    let ret f = async { return Choice1Of2 f }
    let fail f = async { return Choice2Of2 f }
    
    type EtlBuilder() = 
        member x.Bind((name,f), rest) = stage name f rest
        member x.Return(f) = ret f
        member x.ReturnFrom(f) : Async<Choice<_,_>> =  f 
                    
    
    let toAsync successF compensation comp = 
        async {
            let! result = comp
            match result with
            | Choice1Of2 r -> return successF r
            | Choice2Of2 (name,err) -> return compensation name err
        }

    let etl = EtlBuilder()




