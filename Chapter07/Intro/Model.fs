namespace global

open System
open System.ComponentModel
open System.Collections.Generic
open System.Reflection
open Castle.DynamicProxy

module Method = 

    //#A If setter extract property name
    let (|PropertySetter|_|) (m: MethodInfo) =
        match m.Name.Split('_') with
        | [| "set"; propertyName |] -> Some propertyName
        | _ -> None

    //#B If getter extract property name
    let (|PropertyGetter|_|) (m: MethodInfo) =
        match m.Name.Split('_') with
        | [| "get"; propertyName |] -> Some propertyName
        | _ -> None
        
    //#C Is it an abstract method? 
    let (|Abstract|_|) (m: MethodInfo) = if m.IsAbstract then Some() else None

open Method

[<AbstractClass>]
type Model() = 

    static let proxyFactory = ProxyGenerator()
    //#A The interceptor responsible for raising the PropertyChanged event can be static because it’s stateless.
    static let notifyPropertyChanged = {
    //#B Create singleton instance using F# object expression. StandardInterceptor makes it easy to intercept the postprocess step only.
        new StandardInterceptor() with
            member this.PostProceed invocation = 
                match invocation.Method, invocation.InvocationTarget with 
                    | PropertySetter propertyName, (:? Model as model) -> 
                        model.TriggerPropertyChanged propertyName
                    | _ -> ()
    }

    let propertyChangedEvent = Event<_,_>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChangedEvent.Publish

    member internal this.TriggerPropertyChanged propertyName = 
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs propertyName)

    //#C Custom model instances must be created via the Create factory method. It sets up interception.
    static member Create<'T when 'T :> Model and 'T: not struct>(): 'T = 
        //#D notifyPropertyChanged is a static singleton, AbstractProperties — one per model instance.
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
                        //#E For value types return default type value,; for reference types, it’s null.
                        if returnType.IsValueType then 
                            invocation.ReturnValue <- Activator.CreateInstance returnType
                //#F Clever use of pattern matching (for example, Abstract and PropertyGetter) not only makes code readable but also helps avoid duplication — there is only one fallback branch. 
                | _ -> invocation.Proceed()

