open System

open SharpVille.Model
open SharpVille.Server.DAL
open SharpVille.Server.GameEngine
open SharpVille.Server.Http

[<EntryPoint>]
let main argv = 
    let stateRepo = InMemoryStateRepo()
    let sessionStore = InMemorySessionStore()

    let gameSpec = 
        {
            Seeds        = [| 
                              ("S1", { Id = "S1"; RequiredLevel = 1<lvl>; Cost = 10L<gold>; GrowthTime = TimeSpan.FromSeconds 30.0; Yield = 12L<gold>; Exp = 1L<exp> })
                              ("S2", { Id = "S2"; RequiredLevel = 2<lvl>; Cost = 13L<gold>; GrowthTime = TimeSpan.FromSeconds 45.0; Yield = 16L<gold>; Exp = 2L<exp> })
                              ("S3", { Id = "S3"; RequiredLevel = 2<lvl>; Cost = 13L<gold>; GrowthTime = TimeSpan.FromSeconds 60.0; Yield = 17L<gold>; Exp = 3L<exp> })
                              ("S4", { Id = "S4"; RequiredLevel = 3<lvl>; Cost = 15L<gold>; GrowthTime = TimeSpan.FromSeconds 90.0; Yield = 20L<gold>; Exp = 4L<exp> })
                              ("S5", { Id = "S5"; RequiredLevel = 4<lvl>; Cost = 17L<gold>; GrowthTime = TimeSpan.FromSeconds 45.0; Yield = 22L<gold>; Exp = 5L<exp> })
                              ("S6", { Id = "S6"; RequiredLevel = 5<lvl>; Cost = 20L<gold>; GrowthTime = TimeSpan.FromSeconds 60.0; Yield = 26L<gold>; Exp = 6L<exp> })
                           |] 
                           |> Map.ofSeq
            Levels       = [| 
                              (1<lvl>,  0L<exp>)
                              (2<lvl>,  10L<exp>)
                              (3<lvl>,  24L<exp>)
                              (4<lvl>,  42L<exp>)
                              (5<lvl>,  64L<exp>)
                              (6<lvl>,  94L<exp>)
                              (7<lvl>,  139L<exp>)
                              (8<lvl>,  204L<exp>)
                              (9<lvl>,  294L<exp>)
                           |] 
                           |> Map.ofSeq
            DefaultState = 
                {
                    PlayerId      = ""
                    Exp           = 0L<exp>
                    Level         = 1<lvl>
                    Balance       = 100L<gold>
                    FarmDimension = 10, 10
                    Plants        = Map.empty
                }
        }

    let gameEngine = GameEngine(stateRepo, sessionStore, gameSpec) :> IGameEngine

    startServer gameEngine

    Console.ReadKey() |> ignore

    0 // return an integer exit code