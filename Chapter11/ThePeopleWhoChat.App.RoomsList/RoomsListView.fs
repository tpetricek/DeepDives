namespace ThePeopleWhoChat.App.RoomsList

    open System
    open System.Windows
    open System.Windows.Controls

    type RoomsListView() as this =
        inherit UserControl()

        do Application.LoadComponent(this, new Uri("/ThePeopleWhoChat.App.RoomsList;component/RoomsListView.xaml",UriKind.Relative))

        let layoutRoot : Grid = downcast this.FindName("LayoutRoot")