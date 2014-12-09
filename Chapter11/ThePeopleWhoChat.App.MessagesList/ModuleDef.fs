namespace ThePeopleWhoChat.App.MessagesList

    open Microsoft.Practices.Prism.Modularity
    open Microsoft.Practices.Prism.Regions

    type ModuleDef(regionManager: IRegionManager) =

        interface IModule with
            member this.Initialize() =
                //ignore <| regionManager.RegisterViewWithRegion("MessagesList", typeof<MessagesListView>)
                ()

