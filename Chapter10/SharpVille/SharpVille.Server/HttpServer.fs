module SharpVille.Server.Http

open System
open System.IO
open System.Net

open SharpVille.Common.Utils
open SharpVille.Model
open SharpVille.Server.DAL
open SharpVille.Server.GameEngine

let inline handleReq (f : 'req -> 'resp) = 
    (fun (req : HttpListenerRequest) (resp : HttpListenerResponse) -> 
        async {
            let inputStream = req.InputStream
            let request = inputStream |> readJson<'req>
            try
                let response = f request
                writeJson response resp.OutputStream
                resp.OutputStream.Close()
            with
            | _ -> resp.StatusCode <- 500
                   resp.Close()
        })

type HttpListener with
    static member Run (url:string, handler) =
        let listener = new HttpListener()
        listener.Prefixes.Add url
        listener.Start()

        let getContext = Async.FromBeginEnd(listener.BeginGetContext, 
                                            listener.EndGetContext)

        async {
            while true do
                let! context = getContext
                Async.Start (handler context.Request context.Response)
        } |> Async.Start

        listener

let startServer (gameEngine : IGameEngine) =
    HttpListener.Run("http://*:80/SharpVille/Handshake/", 
                     handleReq gameEngine.Handshake) 
    |> ignore

    HttpListener.Run("http://*:80/SharpVille/Plant/", 
                     handleReq gameEngine.Plant)
    |> ignore

    HttpListener.Run("http://*:80/SharpVille/Harvest/", 
                     handleReq gameEngine.Harvest)
    |> ignore