namespace global

open System
open System.ComponentModel

// Listing 9. MVC

// Listing 5. IView interface
type IView<'TEvent, 'TModel> =
    //#A First responsibility: event source
    abstract Events: IObservable<'TEvent>
    //#B Second responsibility: set-up data bindings 
    abstract SetBindings: 'TModel -> unit

type IController<'TEvent, 'TModel> =
    abstract InitModel: 'TModel -> unit
    abstract EventHandler: 'TModel * 'TEvent -> unit

module Mvc = 
    let start (model: #INotifyPropertyChanged, 
                view: IView<_, _>, 
                controller: IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view.Events.Subscribe(fun event -> 
            controller.EventHandler(model, event))

