namespace ThePeopleWhoChat.App

    open System
    open System.Reflection
    open System.IO
    open System.Windows
    open System.Windows.Controls
    open System.Windows.Markup

    type Shell() as this =
        inherit UserControl()

        let sr = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shell.xaml")
        let test = XamlReader.Load(sr)
       // do Application.Current.

        let mutable layoutRoot : Grid = downcast this.FindName("LayoutRoot")