open System

type UpDownEvent = Incr | Decr
type View = IObservable<UpDownEvent>  
type Model = { mutable State : int }
type Controller = Model -> UpDownEvent -> unit
type Mvc = Controller -> Model -> View -> IDisposable

//Event<_> type from the F# core library plays two roles: Observer via Trigger method and Subject (aka event source) because it inherits from IObservable<_><_>.
let subject = Event<UpDownEvent>()
let raiseEvents xs = List.iter subject.Trigger xs
let view = subject.Publish

let model : Model = { State = 6 }

let controller model event = 
    match event with
    | Incr -> model.State <- model.State + 1 
    | Decr -> model.State <- model.State - 1

let mvc : Mvc = fun controller model view -> 
    view.Subscribe(fun event -> 
        controller model event 
        printfn "Model: %A" model)

let subscription = view |> mvc controller model

raiseEvents [Incr; Decr; Incr]

subscription.Dispose()
