// Code from Chapter 5 "Social network analysis"
// Evelina Gabasova, 2014
// from the book F# Deep Dives, 2014
// ==================================================

#r "packages/FSharp.Data.2.0.7/lib/net40/FSharp.Data.dll"
#r "packages/FSharp.Data.Toolbox.Twitter.0.2/lib/net40/FSharp.Data.Toolbox.Twitter.dll"

open System
open System.IO
open System.Threading

open FSharp.Data
open FSharp.Data.Toolbox.Twitter

// Set directory for saving results
let currentDirectory = ""
Directory.SetCurrentDirectory currentDirectory

// Listing 1 - Connecting to Twitter
// ==================================================

// Application credentials
let key = "CoqmPIJ553Tuwe2eQgfKA"
let secret = "dhaad3d7DreAFBPawEIbzesS1F232FnDsuWWwRTUg"

// Full authentication
let connector = Twitter.Authenticate(key, secret)
// A window appers for Twitter sign-in
// After authentication, a PIN should appear
// Use the PIN as an argument for the Connect function
let twitter = connector.Connect("2326296")

// Downloading the data
// ==================================================
// Data used in the chapter were downloaded on 17 February 2014

// Get friends list of the F# Software Foundation
// i.e. accounts that F# foundation follows
let friends = twitter.Connections.FriendsIds(screenName="@fsharporg")
friends.Ids |> Seq.length   

// Get list of followers for @fsharporg
let followers = twitter.Connections.FollowerIds(screenName="@fsharporg")
followers.Ids |> Seq.length   

// Create a set of accounts around @fsharporg with a test for inclusion
let idsOfInterest = Seq.append friends.Ids followers.Ids |> set

// Listing 2 - Twitter screen names from user ID numbers 
// ==================================================

// One lookup request for up to 100 users
// limitted to 180 requests per 15 minutes (with full authentication)
// i.e. 1 request per 5 seconds

// Group IDs into groups by 100 users
let groupedIds = 
    idsOfInterest
    |> Seq.mapi (fun i id -> i/100, id)
    |> Seq.groupBy fst
let ngroups = Seq.toList groupedIds |> List.length

// Download user information
let twitterNodes = 
    [| for _, group in groupedIds do
        let ids = Seq.map snd group
        let nodeInfo = 
            twitter.Users.Lookup(ids)
            |> Array.map (fun node -> node.Id, node.ScreenName)
        yield! nodeInfo |]
    
// Listing 3 - Twitter connections between users
// ==================================================

// Beware, downloading Twitter connections is a long process due to 
// access rate limits. I recommend running this on a server with a stable 
// internet connection. 

let isInNetwork id = idsOfInterest.Contains id

// Get connections from Twitter
let twitterConnections (ids:int64 seq) =
    [|  for srcId in ids do
        Thread.Sleep(60000)     // wait for one minute
        let connections = 
            try 
                // Get IDs of friends and keep
                // only nodes that are connected to @fsharporg
                twitter.Connections.FriendsIds(srcId).Ids
                |> Array.filter isInNetwork 
            with _ -> 
                // accounts with hidden list of friends and followers etc
                printfn "Unable to access ID %i" srcId
                [||]      
        // return source and target
        yield! connections |> Seq.map (fun tgtId -> srcId, tgtId)|]


// Listing 4 - Export network’s nodes into JSON
// =====================================================

let jsonNode (userInfo: int64*string) = 
    let id, name = userInfo
    JsonValue.Record [| 
            "name", JsonValue.String name
            "id", JsonValue.Number (decimal id) |] 

let jsonNodes = 
    let nodes = twitterNodes |> Array.map jsonNode
    [|"nodes", (JsonValue.Array nodes) |]
    |> JsonValue.Record
File.WriteAllText("fsharporgNodes.json", jsonNodes.ToString())

// Listing 5 - Export network’s links into JSON
// ======================================================

// Helper functions to translate between Twitter IDs to zero-based indices
let idToIdx =
    idsOfInterest 
    |> Seq.mapi (fun idx id -> (id, idx))
    |> dict

// Save links in JSON format
let jsonConnections (srcId, tgtId) = 
    let src = idToIdx.[srcId]
    let tgt = idToIdx.[tgtId]
    JsonValue.Record [|
            "source", JsonValue.Number (decimal src)
            "target", JsonValue.Number (decimal tgt) |] 

let jsonLinks = 
    let linkArr =
        twitterConnections idsOfInterest
        |> Array.map jsonConnections
        |> JsonValue.Array
    JsonValue.Record [|"links", linkArr|]
File.WriteAllText(@"fsharporgLinks.json", jsonLinks.ToString())




