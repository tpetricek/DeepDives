namespace global

open System
open System.ComponentModel

type IView<'TEvent, 'TModel> =
    abstract Events : IObservable<'TEvent>
    abstract SetBindings: model : 'TModel -> unit

type IController<'TEvent, 'TModel> =
    abstract InitModel: 'TModel -> unit
    abstract EventHandler: 'TModel * 'TEvent -> unit

module Mvc = 
    let start (model: #INotifyPropertyChanged, view: IView<'TEvent, 'TModel>, controller: IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view.Events.Subscribe(fun event -> controller.EventHandler(model, event))

