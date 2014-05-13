namespace SharpVille.Model.Requests

open System
open SharpVille.Model

type HandshakeRequest =
    {
        PlayerId        : PlayerId
        Hash            : string
    }

type GetLeaderboardRequest =
    {
        SessionId       : SessionId
        Friends         : PlayerId[]
    }

type PlantRequest =
    {
        SessionId       : SessionId
        Position        : Coordinate
        Seed            : SeedId
    }

type HarvestRequest =
    {
        SessionId       : SessionId
        Position        : Coordinate
    }

type VisitRequest =
    {
        SessionId       : SessionId
        Friend          : PlayerId
    }

type FertilizeRequest =
    {
        SessionId       : SessionId
        Friend          : PlayerId
        Position        : Coordinate
    }