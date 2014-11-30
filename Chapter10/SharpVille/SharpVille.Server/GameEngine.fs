module SharpVille.Server.GameEngine

open System

open SharpVille.Model
open SharpVille.Model.Requests
open SharpVille.Model.Responses
open SharpVille.Server.DAL

type IGameEngine =
    abstract member Handshake   : HandshakeRequest -> HandshakeResponse
    abstract member Plant       : PlantRequest     -> PlantResponse
    abstract member Harvest     : HarvestRequest   -> HarvestResponse

type GameEngine (stateRepo      : IStateRepository,
                 sessionStore   : ISessionStore,
                 gameSpec       : GameSpecification) = 
    let getState sessionId =
        match sessionStore.Get sessionId with
        | Some x -> match stateRepo.Get x.PlayerId with
                    | Some x -> x
                    | _      -> failwith "State not found"
        | _      -> failwith "Invalid session"

    let awardExp ({ Exp = currExp; Level = currLvl } as state) exp =
        let newExp = currExp + exp
        let newLvl = gameSpec.Levels 
                     |> Seq.fold (fun acc elem -> 
                        if elem.Key > acc && newExp >= elem.Value 
                        then elem.Key
                        else acc) currLvl
        
        (newExp, newLvl)

    interface IGameEngine with
        member this.Handshake (req : HandshakeRequest) = 
            let session = { Id = Guid.NewGuid(); PlayerId = req.PlayerId }
            sessionStore.Put(session)

            let state = 
                match stateRepo.Get req.PlayerId with 
                | None -> 
                    let state = { gameSpec.DefaultState with 
                                    PlayerId = req.PlayerId }
                    stateRepo.Put(state)
                    state
                | Some x -> x

            HandshakeResponse(state.Exp, state.Level, state.Balance, 
                              state.Plants, 
                              state.FarmDimension, 
                              session.Id, 
                              gameSpec)

        member this.Plant (req : PlantRequest) = 
            let state = getState req.SessionId

            let seed = match gameSpec.Seeds.TryFind req.Seed with
                       | Some seed -> seed
                       | _ -> failwithf "Invalid SeedId : %s" req.Seed

            if state.Level < seed.RequiredLevel then 
                failwith "Insufficient level"
            elif state.Balance < seed.Cost then 
                failwith "Insufficient balance"
            elif state.Plants.ContainsKey req.Position then 
                failwith "Farmplot not empty"

            let newPlant = { 
                              Seed = seed.Id
                              DatePlanted = DateTime.UtcNow 
                           }
            let newExp, newLvl = awardExp state seed.Exp
            let newState = 
                { state with 
                    Balance = state.Balance - seed.Cost
                    Plants  = state.Plants.Add(req.Position, newPlant)
                    Exp     = newExp
                    Level   = newLvl }
            stateRepo.Put(newState)

            PlantResponse(newState.Exp, newState.Level, newState.Balance, 
                          newState.Plants)

        member this.Harvest (req : HarvestRequest) =
            let state = getState req.SessionId

            let plant = match state.Plants.TryFind req.Position with
                        | Some plant -> plant
                        | _          -> failwith "No plants found"

            let seed = gameSpec.Seeds.[plant.Seed]
            if DateTime.UtcNow - plant.DatePlanted < seed.GrowthTime then 
                failwith "Plant not harvestable"

            let newExp, newLvl = awardExp state seed.Exp
            let newState = 
                { state with 
                    Balance = state.Balance + seed.Yield
                    Plants  = state.Plants.Remove(req.Position)
                    Exp     = newExp
                    Level   = newLvl }
            stateRepo.Put(newState)

            HarvestResponse(newState.Exp, newState.Level, newState.Balance, 
                            newState.Plants)