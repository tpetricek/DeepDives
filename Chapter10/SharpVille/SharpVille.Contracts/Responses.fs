namespace SharpVille.Model.Responses

open SharpVille.Model

type StateResponse (exp      : int64<exp>,
                    level    : int<lvl>, 
                    balance  : int64<gold>, 
                    plants   : Map<Coordinate, Plant>) =
    member this.Exp     = exp
    member this.Level   = level
    member this.Balance = balance
    member this.Plants  = plants

type HandshakeResponse (exp, level, balance, plants, 
                        farmDimension : Coordinate, 
                        sessionId     : SessionId,
                        gameSpec      : GameSpecification) =
    inherit StateResponse(exp, level, balance, plants)
    member this.SessionId         = sessionId
    member this.FarmDimension     = farmDimension
    member this.GameSpecification = gameSpec

type PlantResponse (exp, level, balance, plants) =
    inherit StateResponse(exp, level, balance, plants)

type HarvestResponse(exp, level, balance, plants) =
    inherit StateResponse(exp, level, balance, plants)

type LeaderboardFriend =
    {
        Name            : string
        PlayerID        : PlayerId
        Exp             : int64<exp>
    }

type GetLeaderboardResponse =
    {
        Friends         : LeaderboardFriend[]
    }

type VisitResponse =
    {
        Plants          : Map<Coordinate, Plant>
        Fertilizers     : int<fertilizer>
    }

type FertilizeResponse =
    {
        Fertilizers     : int<fertilizer>
    }