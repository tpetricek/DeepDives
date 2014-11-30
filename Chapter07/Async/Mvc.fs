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
    let start (model: #INotifyPropertyChanged, view: IView<_, _>, controller: IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view.Events.Subscribe(fun event -> 
            //#A Dispatcher returns proper handler for particular event. It can be either Sync or Async.
            match controller.Dispatcher event with
            //#B For Sync we you just invoke it as before. 
            | Sync eventHandler -> eventHandler model 
            //#C For Async we you delegate invocation to Async.StartImmediate. 
            | Async eventHandler -> eventHandler model |> Async.StartImmediate
        )

