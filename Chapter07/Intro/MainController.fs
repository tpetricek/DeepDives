namespace global

open System
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type MainContoller(?symbology : string -> string option) = 

    let symbologyImpl = defaultArg symbology Symbology.yahoo

    let closePosition(model : MainModel) = 
        model.ClosePrice <- model.Price
        model.PositionState <- PositionState.Closed
        model.IsPositionActionAllowed <- false

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
            model.PositionSize <- 10
            model.PositionState <- PositionState.Zero
            model.IsPositionActionAllowed <- true

        member this.EventHandler(model, event) = 
            match event with
            | InstrumentInfo -> this.GetInstrumentInfo(model)
            | PriceUpdate newPrice -> this.UpdateCurrentPrice(model, newPrice) 
            | BuyOrSell -> this.MoveToNextPositionState(model)

    member this.GetInstrumentInfo(model : MainModel) = 
        model.Symbol |> symbologyImpl  |> Option.iter (fun x -> model.InstrumentName <- x) 

    member this.UpdateCurrentPrice(model : MainModel, newPrice) =
        let prevPrice = model.Price
        model.Price <- Nullable newPrice
        match model.PositionState with
        | PositionState.Opened -> 
            model.PnL <- 
                let x = decimal model.PositionSize *? ( model.Price ?-? model.OpenPrice)
                x.GetValueOrDefault()
            let takeProfitLimit = prevPrice ?< newPrice && newPrice >=? model.TakeProfitAt
            let stopLossLimit = prevPrice ?> newPrice && newPrice <=? model.StopLossAt
            if takeProfitLimit || stopLossLimit 
            then closePosition model
        | _ -> ()

    member this.MoveToNextPositionState(model : MainModel) = 
        match model.PositionState with
        | PositionState.Zero ->
            model.OpenPrice <- model.Price
            model.PositionState <- PositionState.Opened
        | PositionState.Opened ->
            closePosition model
        | PositionState.Closed -> 
            ()

