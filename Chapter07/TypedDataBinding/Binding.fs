[<RequireQualifiedAccess>]
module Binding

open System
open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

type PropertyInfo with
    member this.DependencyProperty = 
        this.DeclaringType
            .GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public)
            .GetValue(null, [||]) 
            |> unbox<DependencyProperty> 

let (|Target|_|) = function 
    | Some( FieldGet( Some( Value( window, _)), control)) -> 
        window |> control.GetValue |> unbox<DependencyObject> |> Some
    | _ -> None

let coerce _ = raise <| NotImplementedException()

let rec (|BindingInstance|_|) = function 
    | PropertyGet( Some( Value _), sourceProperty, []) -> 
        Some( Binding(path = sourceProperty.Name))
    | SpecificCall <@ coerce @> (None, _, [ BindingInstance binding ]) -> 
        Some binding
    | NewObject( ctorInfo, [ BindingInstance binding ] ) 
        when ctorInfo.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>> -> 
        Some binding 
    | SpecificCall <@ String.Format : string * obj -> string @> (None, [], [ Value(:? string as format, _); Coerce( BindingInstance binding, _) ]) ->
        binding.StringFormat <- format
        Some binding
    | Call(None, method', [ BindingInstance binding ]) -> 
        binding.Mode <- BindingMode.OneWay
        binding.Converter <- {
            new IValueConverter with
                member this.Convert(value, _, _, _) = 
                    try method'.Invoke(null, [| value |]) 
                    with _ -> DependencyProperty.UnsetValue
                member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
        }
        Some binding
    | _ -> None

let rec split = function 
    | Sequential(head, tail) -> head :: split tail
    | tail -> [ tail ]

let fromExpression expr = 
    for e in split expr do
        match e with 
        | PropertySet(Target target, targetProperty, [], BindingInstance binding) ->
            BindingOperations.SetBinding(target, targetProperty.DependencyProperty, binding) |> ignore
        | expr -> failwithf "Invalid binding quotation:\n%O" expr

