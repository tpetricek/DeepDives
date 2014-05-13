module GameState

open System.ComponentModel

open SharpVille.Model

type GameState (playerId : PlayerId) =
    let mutable sessionId : SessionId option         = None
    let mutable gameSpec  : GameSpecification option = None
    let mutable dimension : Coordinate option        = None
    let mutable exp       : int64<exp>  = 0L<exp>
    let mutable level     : int<lvl>    = 0<lvl>
    let mutable balance   : int64<gold> = 0L<gold>
    let mutable plants                  = Map.empty<Coordinate, Plant>

    let event = Event<_, _>()

    member this.PlayerId                  = playerId
    member this.SessionId with get ()     = sessionId
                          and  set value  = sessionId <- value
    member this.GameSpec  with get ()     = gameSpec
                          and  set value  = gameSpec <- value
    member this.Dimension with get ()     = dimension
                          and  set value  = dimension <- value
    member this.Plants    with get ()     = plants
                          and  set value  = plants <- value

    member this.Exp
        with get ()     = exp
        and  set value  = exp <- value
                          event.Trigger(this, PropertyChangedEventArgs("Exp"))
    member this.Level
        with get ()     = level
        and  set value  = level <- value
                          event.Trigger(this, PropertyChangedEventArgs("Level"))
    member this.Balance
        with get ()     = balance
        and  set value  = balance <- value
                          event.Trigger(this, PropertyChangedEventArgs("Balance"))
    
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = event.Publish