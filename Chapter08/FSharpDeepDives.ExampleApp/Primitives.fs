namespace FSharpDeepDives.ExampleApp

open System
open FSharpDeepDives

module Option =
    let map2 op avg1 avg2 = 
        match avg1, avg2 with
        | Some(a), Some(b) -> Some (op a b)
        | Some(a), _ -> Some a
        | _, Some(b) -> Some b
        | _, _ -> None

[<AutoOpen>]
module Primitives =

    [<Measure>]type MW

    /// <summary>
    /// A discriminated union to represent a set of triggers
    /// </summary>
    type Trigger = 
        | Cron
        | File of string

    /// <summary>
    /// The event types available for processing
    /// </summary>
    type EventType =
        | ExtractedData of Trigger * string
        | TransformedData of Trigger * string
        | TotalStatisticUpdated
        | AverageStatisticUpdated

    /// <summary>
    /// A simple representation of a Settlement Period
    /// </summary>
    type SettlementPeriod = DateTime * int

    /// <summary>
    /// A representation of the current Power Generation Stats by fuel
    /// </summary>
    type FuelSettlementPeriod  = {
        Period : SettlementPeriod
        CCGT : int<MW>
        OCGT : int<MW>
        Coal : int<MW>
        Oil : int<MW>
        Wind : int<MW>
        Nuclear : int<MW>
    }

    /// <summary>
    /// A simple message representing incoming data from various sources
    /// </summary>
    type Message = 
         | Data of seq<FuelSettlementPeriod>
    
    /// <summary>
    /// A aggregate type that we can use to compute stastitics by 
    /// fuel type
    /// </summary>
    type FuelTypeStats = {
        CCGT : float<MW>
        OCGT : float<MW>
        Coal : float<MW>
        Oil : float<MW>
        Wind : float<MW>
        Nuclear : float<MW>
    }
    with
        static member Create(fsp : FuelSettlementPeriod) = 
            {
              CCGT = (float fsp.CCGT |> LanguagePrimitives.FloatWithMeasure); 
              OCGT = (float fsp.OCGT |> LanguagePrimitives.FloatWithMeasure); 
              Coal = (float fsp.Coal |> LanguagePrimitives.FloatWithMeasure); 
              Oil = (float fsp.Oil  |> LanguagePrimitives.FloatWithMeasure); 
              Wind = (float fsp.Wind  |> LanguagePrimitives.FloatWithMeasure);
              Nuclear = (float fsp.Nuclear |> LanguagePrimitives.FloatWithMeasure)   
            }
        static member Zero = 
            { CCGT = 0.<MW>; 
              OCGT = 0.<MW>; 
              Coal = 0.<MW>; 
              Oil = 0.<MW>; 
              Wind = 0.<MW>;
              Nuclear = 0.<MW> }
        static member DivideByInt(fsp: FuelTypeStats, count:int) = 
            { fsp with
                  CCGT = fsp.CCGT / (float count)
                  OCGT = fsp.CCGT / (float count)
                  Coal = fsp.Coal / (float count)
                  Oil = fsp.Oil / (float count)
                  Wind = fsp.Wind / (float count)
                  Nuclear = fsp.Nuclear / (float count) 
            } 
        static member (+) (ft1, ft2) = 
            {
                CCGT = ft1.CCGT + ft2.CCGT
                OCGT = ft1.OCGT + ft2.OCGT
                Coal = ft1.Coal + ft2.Coal
                Oil = ft1.Oil + ft2.Oil
                Wind = ft1.Wind + ft2.Wind
                Nuclear = ft1.Nuclear + ft2.Nuclear
            }
