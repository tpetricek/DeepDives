// Code from Chapter 5 "Social network analysis"
// Evelina Gabasova, 2014
// from the book F# Deep Dives, 2014
// ==================================================

#r "packages/FSharp.Data.2.0.7/lib/net40/FSharp.Data.dll"
#r "packages/MathNet.Numerics.3.0.0-beta01/lib/net40/MathNet.Numerics.dll"
#r "packages/MathNet.Numerics.FSharp.3.0.0-beta01/lib/net40/MathNet.Numerics.FSharp.dll"
#r "packages/RProvider.1.0.7-alpha/lib/RProvider.dll"
#r "packages/RProvider.1.0.7-alpha/lib/RProvider.Runtime.dll"
#r "packages/R.NET.1.5.5/lib/net40/RDotNet.dll"
#r "packages/RDotNet.FSharp.0.1.2.1/lib/net40/RDotNet.FSharp.dll"

open System
open System.IO

open MathNet.Numerics
open MathNet.Numerics.LinearAlgebra
open MathNet.Numerics.LinearAlgebra.Double
open RProvider
open RProvider.``base``
open RProvider.graphics
open FSharp.Data

open System
open System.IO
open System.Collections.Generic

// Listing 6 - Loading JSON data with type providers
// ===================================================

let dataDirectory = @"C:\Users\Public\Documents\Projects\SocialNetworkAnalysis\data\"
let nodeFile = dataDirectory + "fsharporgNodes.json"
let linkFile = dataDirectory + "fsharporgLinks.json"

type Users = JsonProvider<"C:\\Users\\Public\\Documents\\Projects\\SocialNetworkAnalysis\\data\\fsharporgNodes.json">
let userNames = Users.Load nodeFile

type Connections = JsonProvider<"C:\\Users\\Public\\Documents\\Projects\\SocialNetworkAnalysis\\data\\fsharporgLinks.json">
let userLinks = Connections.Load linkFile

// Listing 7 - Helper functions for Twitter IDs
// ===================================================
// Read ID numbers and screen names of accounts in @fsharporg network
// To facilitate easy translation between the two
// We do not put @fsharporg into the network because it is connected to all nodes

let idToName = dict [ for node in userNames.Nodes -> node.Id, node.Name ] 
let nameToId = dict [ for node in userNames.Nodes -> node.Name, node.Id ]

// Usage
nameToId.["dsyme"]

// Introduce indices (stargin from 0) to number nodes in the network
let idxToId, idToIdx =
    let idxList, idList =
        userNames.Nodes
        |> Array.mapi (fun idx node -> (idx,node.Id), (node.Id, idx))
        |> Array.unzip
    dict idxList, dict idList

// Translate index to Twitter Id and Name
let idxToIdName idx =
    let id = idxToId.[idx]
    id, idToName.[id]

// Find idex for a specific screen name
let nameToIdx screenName =
    let id = nameToId.[screenName]
    idToIdx.[id]

// Usage
nameToIdx "dsyme"
idxToIdName 213

// Listing 8 - Sparse adjacency matrix
// ===================================================

// Read links in the @fsharporg network into an adjacency matrix
let nodeCount = userNames.Nodes.Length
let links = 
    seq { for link in userLinks.Links -> link.Source, link.Target, 1.0 }
    |> SparseMatrix.ofSeqi nodeCount nodeCount

// Listing 9 - Out-degree and in-degree
// ===================================================

let outdegree (linkMatrix:float Matrix) =
    [| for outlinks in linkMatrix.EnumerateRows() -> outlinks.Sum() |]   

let indegree (linkMatrix: float Matrix) =
    [| for inlinks in linkMatrix.EnumerateColumns() -> inlinks.Sum() |]

let degree linkMatrix = Array.map2 (+) (outdegree linkMatrix) (indegree linkMatrix)

// Listing 10  - In-degree and out-degree with matrix multiplication
// ====================================================================

let outdegreeFaster (linkMatrix: float Matrix) =
    linkMatrix * DenseMatrix.Create(linkMatrix.RowCount, 1, 1.0)

let indegreeFaster (linkMatrix: float Matrix) =
    DenseMatrix.Create(1, linkMatrix.ColumnCount, 1.0) * linkMatrix

#time
let indegrees = indegree links
let outdegrees = outdegree links
outdegreeFaster links

let degrees = degree links

// Listing 11 - Top users from a ranking
// ===================================================
// Find top ranking users
let topUsers (ranking:float seq) count = 
    ranking
    |> Seq.mapi (fun i x -> (i,x))
    |> Seq.sortBy (fun (i,x) -> - x)
    |> Seq.take count
    |> Seq.map (fun (i,x) -> 
        let id, name = idxToIdName i
        (id, name, x))

// Get a list of people that have most followers
topUsers indegrees 100
|> Seq.iteri (fun i (id, name, value) ->
    printfn "%d. %s has indegree %.0f" (i+1) name value)    

// Listing 12 - Degree distribution of nodes
// ==============================================
// Visualize degree distribution with R provider
R.plot(indegrees)

// Log-log plot of degree distribution
let degreeDist ds = ds |> Seq.countBy id
    
let degreeValues, degreeCounts = 
    degreeDist indegrees 
    |> List.ofSeq |> List.unzip

namedParams [
    "x", box degreeValues;
    "y", box degreeCounts;
    "log", box "xy";
    "pch", box 16;
    "col", box "royalblue";
    "xlab", box "Log degree";
    "ylab", box "Log frequency" ]
    |> R.plot


// Listing 13 - Transition matrix
// ===================================================

// Transition matrix - gives transition probabilities
// T[i,j] = probability of transition from i to j
//        = 1/(outdegree[i])
let transitionBasic = 
    seq { for i, j, _ in links.EnumerateNonZeroIndexed() -> 
            i, j, 1.0/outdegrees.[i] }
    |> SparseMatrix.ofSeqi nodeCount nodeCount

// How many dangling nodes are there?
outdegrees
|> Seq.countBy ((=) 1.0)

// Correct for dangling nodes (nodes with no outcoming links)
let transitionMatrix =
    seq { for r, row in transitionBasic.EnumerateRowsIndexed() ->
            // if there are no outgoing links, create links to all
            // other nodes in the network with equal probability
            if row.Sum() = 0.0 then
                SparseVector.init nodeCount (fun i -> 
                        1.0/(float nodeCount))
            else row }
    |> SparseMatrix.ofRowSeq

// Listing 14 - Mapper and reducer functions
// ===================================================
// MapReduce in steps
// 1) Map 
let mapper (transitionMatrix:Matrix<float>) (pageRank:float []) = 
    seq { for (src, tgt, p) in transitionMatrix.EnumerateNonZeroIndexed() do
            yield (tgt, pageRank.[src]*p) 
          for node in 0..transitionMatrix.RowCount-1 do
            yield (node, 0.0) }
    
// 2) Reduce
// random jump factor
let d = 0.85 

let reducer nodeCount (mapperOut: (int*float) seq) = 
    mapperOut
    |> Seq.groupBy fst  // group by node
    |> Seq.sortBy fst
    |> Seq.map (fun (node, inRanks) ->
        let inRankSum = inRanks |> Seq.sumBy snd
        d * inRankSum + (1.0-d)/(float nodeCount))
    |> Seq.toArray

// Listing 15 - PageRank algorithm
// ===========================================================
// Create a vector to hold the page rank values
// and initialize with equal values (1/number of nodes)
// (equal probability of being in any node in the network)
let startPageRank = Array.create nodeCount (1.0/(float nodeCount))
let minDifference = 1e-6  
let maxIter = 100

let rec pageRank iters 
        (transitionMatrix:Matrix<float>) 
        (pageRankVals : float []) = 
    if iters = 0 then pageRankVals
    else
        let nodeCount = transitionMatrix.RowCount
    
        let newPageRanks = 
            pageRankVals
            |> mapper transitionMatrix
            |> reducer nodeCount

        let difference = 
            Array.map2 (fun r1 r2 -> abs (r1 - r2)) 
                pageRankVals newPageRanks
            |> Array.sum
        if difference < minDifference then
            printfn "Converged in iteration %i" (maxIter - iters)
            newPageRanks
        else pageRank (iters-1) transitionMatrix newPageRanks
            
// Test on a small toy network
let smallTransMatrix = 
    [ (0,0,1.0/3.0); (0,1,1.0/3.0); (0,2,1.0/3.0);
      (1,0,1.0); (2,0,0.5); (2,1,0.5) ]
    |> SparseMatrix.ofListi 3 3
let smallPR = pageRank 100 smallTransMatrix (Array.create 3 (1.0/3.0))

// Run on the full transition matrix
let pageRankValues = pageRank maxIter transitionMatrix startPageRank

topUsers pageRankValues 100
|> Seq.iteri (fun i (id, name, value) ->
    printfn "%d. %s has PageRank %f" (i+1) name value)    

// Listing 16 - JSON file for nodes with PageRank information
// ==========================================================

let jsonUsersPR userIdx userPR = 
    let id, name = idxToIdName userIdx
    JsonValue.Record [|
        "name", JsonValue.String name
        "id", JsonValue.Number (decimal id)
        "r", JsonValue.Float userPR |]

let jsonNodes = 
    let jsonPR = Array.mapi (fun idx rank -> jsonUsersPR idx rank) pageRankValues
    JsonValue.Record [| "nodes", (JsonValue.Array jsonPR) |]      

File.WriteAllText("pageRankNodes.json", jsonNodes.ToString())
