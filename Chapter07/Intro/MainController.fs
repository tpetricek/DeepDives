namespace global

open System
open Microsoft.FSharp.Linq
open Linq.NullableOperators

//A symbology Symbology callback is example of external dependency. Can be interface or abstract type as well.
type MainContoller(?symbology: string -> string option) = 

//Default implementation is used unless overridden (for example, for testing purposes) 
    let symbologyImpl = defaultArg symbology Symbology.yahoo

    let closePosition(model: MainModel) = 
        model.ClosePrice <- model.Price
        model.PositionState <- PositionState.Closed
        model.IsPositionActionAllowed <- false

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
//Defaults to S&P 500 index. 
            model.Symbol <- "^GSPC"                                          
//External dependency used to initialize model. 
            this.GetInstrumentInfo model                                     

            model.PositionSize <- 10
            model.PositionState <- PositionState.Zero
            model.IsPositionActionAllowed <- true

        member this.EventHandler(model, event) = 
            match event with
            | InstrumentInfo -> this.GetInstrumentInfo(model)
            | PriceUpdate newPrice -> this.UpdateCurrentPrice(model, newPrice) 
            | BuyOrSell -> this.MoveToNextPositionState(model)

    member this.GetInstrumentInfo(model: MainModel) = 
        model.Symbol |> symbologyImpl  |> Option.iter (fun x -> model.InstrumentName <- x) 

//Based on new price, updates metrics and keeps position value within limits
    member this.UpdateCurrentPrice(model: MainModel, newPrice) =
        let prevPrice = model.Price
        model.Price <- Nullable newPrice
        if model.PositionState = PositionState.Opened 
        then 
            model.PnL <- 
                let x = decimal model.PositionSize *? ( model.Price ?-? model.OpenPrice)
                x.GetValueOrDefault()
            let takeProfitLimit = prevPrice ?< newPrice && newPrice >=? model.TakeProfitAt
            let stopLossLimit = prevPrice ?> newPrice && newPrice <=? model.StopLossAt
            if takeProfitLimit || stopLossLimit 
            then closePosition model

//Enter or exit position 
    member this.MoveToNextPositionState(model: MainModel) = 
        match model.PositionState with
        | PositionState.Zero ->
            model.OpenPrice <- model.Price
            model.PositionState <- PositionState.Opened
        | PositionState.Opened ->
            closePosition model
        | PositionState.Closed -> 
            ()

