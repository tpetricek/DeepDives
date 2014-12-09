namespace ThePeopleWhoChat.App

    open System
    open System.Windows
    open Microsoft.Practices.Prism.UnityExtensions
    open Microsoft.Practices.Prism.Modularity

    type Bootstrapper() =
        inherit UnityBootstrapper() 

        override this.CreateShell() =
            new Shell() :> DependencyObject

        override this.InitializeShell() =
            base.InitializeShell()
            Application.Current.MainWindow <- this.Shell :?> Window
            Application.Current.MainWindow.Show()
            
        override this.CreateModuleCatalog() =
            ModuleCatalog.CreateFromXaml(new Uri("/ThePeopleWhoChat.App;component/modules.xaml", UriKind.Relative)) :> IModuleCatalog