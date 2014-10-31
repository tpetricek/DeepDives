namespace global

open System
open System.ComponentModel
open System.Collections.Generic
open System.Reflection
open Castle.DynamicProxy

module Method = 

    let (|PropertySetter|_|) (m: MethodInfo) =
        match m.Name.Split('_') with
        | [| "set"; propertyName |] -> Some propertyName
        | _ -> None

    let (|PropertyGetter|_|) (m: MethodInfo) =
        match m.Name.Split('_') with
        | [| "get"; propertyName |] -> Some propertyName
        | _ -> None

    let (|Abstract|_|) (m: MethodInfo) = if m.IsAbstract then Some() else None

open Method

[<AbstractClass>]
type Model() = 

    static let proxyFactory = ProxyGenerator()

    static let notifyPropertyChanged = {
        new StandardInterceptor() with
            member this.PostProceed invocation = 
                match invocation.Method, invocation.InvocationTarget with 
                    | PropertySetter propertyName, (:? Model as model) -> 
                        model.TriggerPropertyChanged propertyName
                        model.SetErrors(propertyName, Array.empty)
                    | _ -> ()
    }

    let propertyChangedEvent = Event<_,_>()
    let errors = Dictionary()
    let errorsChanged = Event<_,_>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    member internal this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    interface INotifyDataErrorInfo with
        member this.HasErrors = 
            errors.Values |> Seq.collect id |> Seq.exists (not << String.IsNullOrEmpty)
        member this.GetErrors propertyName = 
            if String.IsNullOrEmpty propertyName 
            then upcast errors.Values 
            else upcast (match errors.TryGetValue propertyName with | true, errors -> errors | false, _ -> Array.empty)
        [<CLIEvent>]
        member this.ErrorsChanged = errorsChanged.Publish

    member this.SetErrors(propertyName, [<ParamArray>] messages: string[]) = 
        errors.[propertyName] <- messages
        errorsChanged.Trigger(this, DataErrorsChangedEventArgs propertyName)

    static member Create<'T when 'T :> Model and 'T: not struct>(): 'T = 
        let interceptors: IInterceptor[] = [| notifyPropertyChanged; AbstractProperties() |]
        proxyFactory.CreateClassProxy interceptors    

and AbstractProperties() =
    let data = Dictionary()

    interface IInterceptor with
        member this.Intercept invocation = 
            match invocation.Method with 
                | Abstract & PropertySetter propertyName -> 
                    data.[propertyName] <- invocation.Arguments.[0]

                | Abstract & PropertyGetter propertyName ->
                    match data.TryGetValue propertyName with 
                    | true, value -> invocation.ReturnValue <- value 
                    | false, _ -> 
                        let returnType = invocation.Method.ReturnType
                        if returnType.IsValueType then 
                            invocation.ReturnValue <- Activator.CreateInstance returnType

                | _ -> invocation.Proceed()

