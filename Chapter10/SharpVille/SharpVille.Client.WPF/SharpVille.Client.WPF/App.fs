module MainApp

open System
open System.IO
open System.Net.Http
open System.Threading
open System.Windows
open System.Windows.Controls
open System.Windows.Media
open System.Windows.Shapes
open System.Windows.Threading

open FSharpx

open SharpVille.Common
open SharpVille.Model
open SharpVille.Model.Requests
open SharpVille.Model.Responses

open GameState
open Utils

type MainWindow = XAML<"MainWindow.xaml">

let player = "test_player"

let emptyPlotBrush   = Brushes.ForestGreen
let plantedPlotBrush = Brushes.DarkGreen

let window = new MainWindow()
let root   = window.Root

let gameState = GameState(player)
root.DataContext <- gameState

let updateState syncContext (response : StateResponse) =
    async {
        gameState.Balance <- response.Balance
        gameState.Exp     <- response.Exp
        gameState.Level   <- response.Level
        gameState.Plants  <- response.Plants    

        let expProgress = 
            match gameState.GameSpec with
            | Some { Levels = lvls } -> 
                let currLvlExp = lvls.[gameState.Level]
                match lvls.TryFind (gameState.Level + 1<lvl>) with 
                | Some nxtLvlExp -> 
                    let prog = float (gameState.Exp - currLvlExp) 
                    let toNextLvl = float (nxtLvlExp - currLvlExp)
                    prog / toNextLvl
                | _ -> 0.0
            | _ -> 0.0

        do! Async.SwitchToContext syncContext
        let fullExpBarWidth = (window.NextLevel :?> Rectangle).Width
        let expBarWidth = fullExpBarWidth * expProgress
        (window.Exp :?> Rectangle).Width <- expBarWidth
    }    

let handshake syncContext = doHandshake player (fun resp ->
    gameState.Dimension <- Some resp.FarmDimension
    gameState.SessionId <- Some resp.SessionId
    gameState.GameSpec  <- Some resp.GameSpecification

    updateState syncContext resp)

let plant x y syncContext   = 
    doPlant x y gameState.SessionId.Value "S1" <| updateState syncContext
let harvest x y syncContext = 
    doHarvest x y gameState.SessionId.Value    <| updateState syncContext

let (|Plant|_|) coordinate (gameState : GameState) = 
    gameState.Plants.TryFind coordinate

let getPlotText x y =
    match gameState with
    | Plant (x, y) plant
        -> let dueDate = plant.DatePlanted.AddSeconds 30.0
           let now = DateTime.UtcNow
           if now >= dueDate then "Harvest"
           else sprintf "%ds" (dueDate - now).Seconds
    | _ -> "Plant"

let onClick x y syncContext (plot : Border) =
    async { 
        match gameState with
        | Plant (x, y) plant
            -> let dueDate = plant.DatePlanted.AddSeconds 30.0
               let now = DateTime.UtcNow
               if now >= dueDate
               then do! harvest x y syncContext
                    do! Async.SwitchToContext syncContext
                    plot.Background <- emptyPlotBrush
                    do! Async.SwitchToThreadPool()
        | _ -> do! plant x y syncContext
               do! Async.SwitchToContext syncContext
               plot.Background <- plantedPlotBrush
               do! Async.SwitchToThreadPool()
    }

let setUpFarmPlots syncContext (container : Grid) =  
    async {
        let (Some (rows, cols)) = gameState.Dimension

        do! Async.SwitchToContext syncContext

        let plotWidth  = container.Width / float rows
        let plotHeight = container.Height / float cols

        { 0..rows-1 } |> Seq.iter (fun _ ->
             new RowDefinition(Height = new GridLength(plotHeight)) 
             |> container.RowDefinitions.Add)
        { 0..cols-1 } |> Seq.iter (fun _ -> 
            new ColumnDefinition() |> container.ColumnDefinitions.Add)

        for rowNum = 0 to rows - 1 do
            for colNum = 0 to cols - 1 do
                let plot = new Border()
                plot.Width  <- plotWidth
                plot.Height <- plotHeight
                plot.Background      <- 
                    match gameState with
                    | Plant (rowNum, colNum) _ -> plantedPlotBrush
                    | _ -> emptyPlotBrush
                plot.BorderBrush     <- Brushes.Black
                plot.BorderThickness <- new Thickness(0.0)
         
                plot.MouseEnter.Add(fun evt -> 
                    plot.BorderThickness <- new Thickness(2.0))
                plot.MouseEnter.Add(fun evt -> 
                    let label = new Label()
                    label.Content <- getPlotText rowNum colNum
                    plot.Child <- label)

                plot.MouseDown.Add(fun evt -> 
                    onClick rowNum colNum syncContext plot |> Async.Start)

                plot.MouseLeave.Add(fun evt -> 
                    plot.BorderThickness <- new Thickness(0.0))
                plot.MouseLeave.Add(fun evt -> plot.Child <- null)
            
                Grid.SetRow(plot, rowNum)
                Grid.SetColumn(plot, colNum)

                container.Children.Add plot |> ignore
    }

let loadWindow() = 
    let syncContext = 
        new DispatcherSynchronizationContext(Application.Current.Dispatcher)
    Threading.SynchronizationContext.SetSynchronizationContext(syncContext)

    async {
        do! handshake syncContext
        do! setUpFarmPlots syncContext window.FarmPlotContainer
    } |> Async.Start

    window.Root

[<STAThread>]
(new Application()).Run(loadWindow()) |> ignore