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

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
            model.PositionState <- PositionState.Zero

        member this.Dispatcher = function 
            | InstrumentInfo -> Async this.GetInstrumentInfo
            | PriceUpdate newPrice -> Sync(this.UpdateCurrentPrice newPrice)
            | BuyOrSell -> Sync this.MoveToNextPositionState

    member this.GetInstrumentInfo(model : MainModel) = 
        async {
            model |> Validation.textRequired <@ fun m -> m.Symbol @>
            if model.IsValid
            then 
                let context = SynchronizationContext.Current
                let! secInfo = symbologyImpl model.Symbol
                do! Async.SwitchToContext context
                match secInfo with
                | Some x -> 
                    model.InstrumentName <- x
                | None -> 
                    let message = "Invalid symbol: " + model.Symbol
                    model |> Validation.setError <@ fun m -> m.Symbol @> message
        }

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
        let prevPrice = model.Price
        model.Price <- Nullable newPrice
        match model.PositionState with
        | PositionState.Opened -> 
            let takeProfitLimit = prevPrice ?< newPrice && newPrice >=? model.TakeProfitAt
            let stopLossLimit = prevPrice ?> newPrice && newPrice <=? model.StopLossAt
            if takeProfitLimit || stopLossLimit 
            then closePosition model
        | _ -> ()

    member this.MoveToNextPositionState (model : MainModel) = 
        match model.PositionState with
        | PositionState.Zero ->
            model |> Validation.positive <@ fun m -> m.PositionSize @>
            model |> Validation.valueRequired <@ fun m -> m.StopLossAt @>
            if model.StopLossAt ?<? (model.Price ?* (80M / 100M))
            then 
                Validation.setErrorf model <@ fun m -> m.StopLossAt @> "Stop loss cannot be lesser than %M%% of current price" 80M
            if model.IsValid 
            then 
                model.OpenPrice <- model.Price
                model.PositionState <- PositionState.Opened
        | PositionState.Opened ->
            closePosition model
        | PositionState.Closed -> 
            ()

