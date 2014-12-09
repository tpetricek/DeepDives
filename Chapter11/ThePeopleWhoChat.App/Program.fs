module Program

    open System
    open System.Collections.ObjectModel
    open System.IO
    open System.Windows
    open System.Windows.Controls
    open System.Windows.Markup
    open ThePeopleWhoChat.App

    [<STAThread>]
    [<EntryPoint>]
    let main(_) =
        let app = new Application()
        app.Startup.Add( fun e -> (new Bootstrapper()).Run() )
        app.Run()
