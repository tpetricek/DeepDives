module Symbology

open System

let yahoo symbol =
    use wc = new Net.WebClient()
    let uri = sprintf "http://download.finance.yahoo.com/d/quotes.csv?s=%s&f=nl1" symbol 
    wc.DownloadString(uri)
        .Split([| "\n\r" |], StringSplitOptions.RemoveEmptyEntries) 
        |> Array.map (fun line -> 
            let xs = line.Split(',')
            let name, price = xs.[0], xs.[1]
            if price = "0.00" then None else Some name
        )
        |> Seq.exactlyOne
