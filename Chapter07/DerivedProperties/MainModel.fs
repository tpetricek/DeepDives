namespace global

open System
open Linq.NullableOperators

type PositionState = Zero | Opened | Closed

[<AbstractClass>]
type MainModel() =
    inherit Model() 
    
    abstract Symbol: string with get, set
    abstract InstrumentName: string with get, set
    abstract Price: Nullable<decimal> with get, set
    abstract PriceFeedSimulation: bool with get, set
    abstract PositionState: PositionState with get, set
    abstract PositionSize: int with get, set
    abstract OpenPrice: Nullable<decimal> with get, set    
    abstract ClosePrice: Nullable<decimal> with get, set
    abstract StopLossAt: Nullable<decimal> with get, set
    abstract TakeProfitAt: Nullable<decimal> with get, set

    [<Binding.DerivedProperty>]
    member this.PnL = 
        let price = 
            if this.ClosePrice.HasValue 
            then this.ClosePrice 
            else this.Price 
        let x = decimal this.PositionSize *? (price ?-? this.OpenPrice)
        x.GetValueOrDefault()

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MainModel =

    let positionStateToAction = function
        | Zero -> "Buy" 
        | Opened -> "Sell" 
        | Closed -> "Current"

    type PositionState with
        member this.ActionAllowed = this <> Closed

    open System.Windows.Media

    type MainModel with
        [<Binding.DerivedProperty>]
        member this.PnlColor = 
            let color = 
                if this.PnL < 0M then "Red"
                elif this.PnL > 0M then "Green" 
                else "Black"
            BrushConverter().ConvertFromString color |> unbox<Brush>

        //Dedicated property that does conversion 
        //is alternative for String.Format
        [<Binding.DerivedProperty>]
        member this.PnlAsString = sprintf "%M" this.PnL
     