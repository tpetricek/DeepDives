namespace FSharpDeepDives.ExampleApp

open System
open FSharpDeepDives
open FSharpDeepDives.Etl

module Elexon =

    let private apiKey = "<your api key here>" //API key available from http://www.elexon.co.uk
    let private yearGenByFuelTypeUrl = "https://downloads.elexonportal.co.uk/file/download/LATESTFUELHHFILE"

    /// <summary>
    /// Provided type to represent the Power Generation by fuel type
    /// </summary>
    type private YTDFuelByHalfHour = FSharp.Data.CsvProvider<"fuelhh_2013.txt">

    /// <summary>
    /// Parses a string to a Power Generation type
    /// </summary>
    /// <param name="input">The input string to parse</param>
    let transformYtdFuelByHalfHour (input:string) = 
        async {
            let rows = YTDFuelByHalfHour.Parse(input).Rows
            return rows
                   |> Seq.toArray
                   |> Array.map (fun row -> 
                                       {
                                           Period = (row.``#Settlement Date``, row.``Settlement Period``)
                                           CCGT = row.CCGT |> LanguagePrimitives.Int32WithMeasure
                                           OCGT = row.OCGT |> LanguagePrimitives.Int32WithMeasure
                                           Coal = row.COAL |> LanguagePrimitives.Int32WithMeasure
                                           Oil = row.OIL |> LanguagePrimitives.Int32WithMeasure
                                           Wind = row.WIND |> LanguagePrimitives.Int32WithMeasure
                                           Nuclear = row.NUCLEAR |> LanguagePrimitives.Int32WithMeasure
                                       })
        }

    /// <summary>
    /// Creates an async function which extracts the Power Generation Data.
    /// The actual source depends on the typ of trigger being passed in
    /// </summary>
    /// <param name="trigger">The trigger that cuased the extraction to be run.</param>
    let extractYtdFuelByHalfHour trigger =
        match trigger with
        | Cron -> DataAccess.getHttp yearGenByFuelTypeUrl ["key", apiKey]
        | File(path) -> DataAccess.getFile path
    
    /// <summary>
    /// The ETL pipeline that represents the Elexon Data Source
    /// </summary>
    /// <param name="dispatcher">A function to route the transformed data</param>
    /// <param name="source">The trigger that started this run.</param>
    let run (dispatcher:_ -> Async<unit>) (source:Trigger) = 
        etl {
            let! extractedData = Agent.pipelined (Agent.bounded "extract YTD Fuel" 5 extractYtdFuelByHalfHour) source
            do! "storing extracted data", DataAccess.storeEvent (ExtractedData(source, "Elexon YTD Fuel")) extractedData
            let! transformedData = Agent.pipelined (Agent.bounded "parse YTD Fuel" 5 transformYtdFuelByHalfHour) extractedData
            do! "storing transformed data", DataAccess.storeEvent (TransformedData(source, "Elexon YTD Fuel")) transformedData
            return (dispatcher transformedData |> Async.StartImmediate)
        }
