namespace global

open System

//Listing 4 Sample model
type PositionState = Zero | Opened | Closed

[<AbstractClass>]
type MainModel() =
    inherit Model() 
    
    abstract Symbol: string with get, set
    abstract InstrumentName: string with get, set
    abstract Price: Nullable<decimal> with get, set
    abstract PriceFeedSimulation: bool with get, set
    abstract PositionState: PositionState with get, set
    abstract IsPositionActionAllowed: bool with get, set
    abstract PositionSize: int with get, set
    abstract OpenPrice: Nullable<decimal> with get, set    
    abstract ClosePrice: Nullable<decimal> with get, set
    abstract PnL: decimal with get, set
    abstract StopLossAt: Nullable<decimal> with get, set
    abstract TakeProfitAt: Nullable<decimal> with get, set

