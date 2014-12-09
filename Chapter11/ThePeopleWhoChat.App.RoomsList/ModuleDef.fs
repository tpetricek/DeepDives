namespace ThePeopleWhoChat.App.RoomsList

    open Microsoft.Practices.Prism.Modularity
    open Microsoft.Practices.Prism.Regions
    open Microsoft.Practices.Unity

    type ModuleDef(container: IUnityContainer, regionManager: IRegionManager) =

        interface IModule with
            member this.Initialize() =
                ignore <| regionManager.RegisterViewWithRegion("RoomsList", fun () -> container.Resolve<RoomsListView>() :> obj)
                ()

