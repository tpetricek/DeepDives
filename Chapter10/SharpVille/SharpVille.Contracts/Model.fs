namespace SharpVille.Model

open System

type X          = int
type Y          = int
type SeedId     = string
type PlayerId   = string
type SessionId  = Guid
type Coordinate = X * Y

[<Measure>]
type exp

[<Measure>]
type lvl

[<Measure>]
type gold

[<Measure>]
type fertilizer

type Session = 
    {
        Id          : SessionId
        PlayerId    : PlayerId
    }

type Plant = 
    {
        Seed        : SeedId
        DatePlanted : DateTime
    }

type State = 
    {
        PlayerId        : PlayerId
        Exp             : int64<exp>
        Level           : int<lvl>
        Balance         : int64<gold>
        FarmDimension   : Coordinate
        Plants          : Map<Coordinate, Plant>
    }

type Seed =
    {
        Id              : SeedId
        RequiredLevel   : int<lvl>
        Cost            : int64<gold>
        GrowthTime      : TimeSpan
        Yield           : int64<gold>
        Exp             : int64<exp>
    }

type GameSpecification =
    {
        Seeds           : Map<SeedId, Seed>
        Levels          : Map<int<lvl>, int64<exp>>
        DefaultState    : State
    }