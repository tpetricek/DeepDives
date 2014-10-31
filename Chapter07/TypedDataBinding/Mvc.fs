namespace global

open System
open System.ComponentModel

type IView<'TEvent, 'TModel> =
    abstract Events: IObservable<'TEvent>
    abstract SetBindings: model: 'TModel -> unit

type EventHandler<'TModel> = 
    | Sync of ('TModel -> unit)
    | Async of ('TModel -> Async<unit>)

type IController<'TEvent, 'TModel> =
    abstract InitModel: 'TModel -> unit
    abstract Dispatcher: ('TEvent -> EventHandler<'TModel>)

module Mvc = 
    let start (model: #INotifyPropertyChanged, view: IView<'TEvent, 'TModel>, controller: IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view.Events.Subscribe(fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler -> eventHandler model 
            | Async eventHandler -> eventHandler model |> Async.StartImmediate
        )

