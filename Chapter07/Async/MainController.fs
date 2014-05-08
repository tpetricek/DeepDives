namespace global

open System
open System.Threading
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type MainContoller(?symbology : string -> Async<string option>) = 

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

        member this.Dispatcher = function 
            | InstrumentInfo -> Async this.GetInstrumentInfo
            | PriceUpdate newPrice -> Sync(this.UpdateCurrentPrice newPrice)
            | BuyOrSell -> Sync this.MoveToNextPositionState

    member this.GetInstrumentInfo(model : MainModel) = 
        async {
            let context = SynchronizationContext.Current
            let! secInfo = symbologyImpl model.Symbol
            do! Async.SwitchToContext context
            secInfo |> Option.iter (fun x -> model.InstrumentName <- x) 
        }

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
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

