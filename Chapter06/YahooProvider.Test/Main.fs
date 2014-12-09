open DeepDives

type tech = Yahoo.Technology
type goods = Yahoo.``Consumer Goods``

let companies =
  [ tech .``Technical & System Software``.``Adobe Systems Inc``
    tech.``Internet Information Providers``.``Google Inc.``
    goods.``Electronic Equipment``.``Apple Inc`` ]

for c in companies do
  let latests = c |> Seq.maxBy (fun r -> r.Date)
  printfn "%A" latests.Open