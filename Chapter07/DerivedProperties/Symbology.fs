module Symbology

open System
open System.Net

let yahoo symbol =
    async {
        use wc = new WebClient()
        let uri = Uri(sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=nl1" symbol) 
        let! response = wc.AsyncDownloadString uri
        return
            response.Split([| Environment.NewLine |], StringSplitOptions.RemoveEmptyEntries) 
            |> Array.map (fun line -> 
                let xs = line.Split(',')
                if xs.[0] = sprintf "\"%s\"" symbol && xs.[1] = "0.00"
                then None
                else Some xs.[0]
            )
            |> Seq.exactlyOne
    }
