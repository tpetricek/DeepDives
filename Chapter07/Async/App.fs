module App

open System
open System.Windows
open UIElements

[<STAThread>]
[<EntryPoint>]
let main _ = 
    let mainWindow = UIElements.MainWindow()
    let mvc = Model.Create(), MainView mainWindow, MainContoller()
    use eventLoop = Mvc.start mvc
    Application().Run mainWindow

