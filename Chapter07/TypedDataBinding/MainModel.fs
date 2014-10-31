namespace global

open System

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
    abstract PnL: decimal with get, set
    abstract StopLossAt: Nullable<decimal> with get, set
    abstract TakeProfitAt: Nullable<decimal> with get, set

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<AutoOpen>]
module MainModel =

    let positionStateToAction = function
        | Zero -> "Buy" 
        | Opened -> "Sell" 
        | Closed -> "Current"

    type PositionState with
        member this.ActionAllowed = this <> Closed
