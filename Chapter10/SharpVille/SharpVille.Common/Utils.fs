namespace SharpVille.Common

open System.Text

[<AutoOpen>]
module Utils =
    open System.IO
    open System.Runtime.Serialization.Json

    let readJson<'a> (stream : Stream) = 
        let serializer = new DataContractJsonSerializer(typedefof<'a>)
        serializer.ReadObject(stream) :?> 'a

    let writeJson (obj : 'a) (stream : Stream) =
        let serializer = new DataContractJsonSerializer(typedefof<'a>)
        serializer.WriteObject(stream, obj) |> ignore