[<RequireQualifiedAccess>]
module Binding

open System
open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Quotations.ExprShape

type PropertyInfo with
    member this.DependencyProperty = 
        this.DeclaringType
            .GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public)
            .GetValue(null, [||]) 
            |> unbox<DependencyProperty> 

let rec (|PropertyPathAndSourceType|_|) = function 
    | PropertyGet( Some( Var dataContext), prop, []) -> Some( prop.Name, dataContext.Type)
    | PropertyGet( Some( Value(_, dataContextType)), prop, []) -> Some( prop.Name, dataContextType)
    | _ -> None

let rec expand vars expr = 

    let expanded = 
        match expr with
            | ShapeVar v when Map.containsKey v vars -> vars.[v]
            | ShapeVar v -> Expr.Var v
            | Call(body, MethodWithReflectedDefinition meth, args) ->
                let this = match body with Some b -> Expr.Application(meth, b) | _ -> meth
                let res = Expr.Applications(this, [ for a in args -> [a]])
                expand vars res
            | PropertyGet(body, PropertyGetterWithReflectedDefinition meth, []) -> 
                let this = match body with Some b -> Expr.Application(meth, b) | _ -> meth
                expand vars (Expr.Application(this, <@@ () @@>))
            | ShapeLambda(v, expr) -> 
                Expr.Lambda(v, expand vars expr)
            | ShapeCombination(o, exprs) ->
                ExprShape.RebuildShapeCombination(o, List.map (expand vars) exprs)

    match expanded with
    | Patterns.Application(ExprShape.ShapeLambda(v, body), assign)
    | Patterns.Let(v, assign, body) ->
        expand (Map.add v (expand vars assign) vars) body
    | _ -> expanded

let rec extractDependencies (self : Var) propertyBody = 
    seq {
        match propertyBody with 
        | PropertyPathAndSourceType(path, dataContextType) -> 
            assert (self.Type = dataContextType)
            yield path
        | ShapeVar _ -> ()
        | ShapeLambda(_, body) -> yield! extractDependencies self body   
        | ShapeCombination(_, exprs) -> for subExpr in exprs do yield! extractDependencies self subExpr
    }

let getPropertyDependencies(model, propertyBody) = 
    propertyBody
        |> expand Map.empty
        |> extractDependencies model
        |> Seq.distinct 
        |> Seq.toList

let getMultiBindingForDerivedProperty(model : Var, body : Expr, getter : obj -> obj) = 
    let binding = MultiBinding(ValidatesOnNotifyDataErrors = false)
    let self = Binding() 
    binding.Bindings.Add self

    for path in getPropertyDependencies(model, body) do
        assert (path <> null)
        binding.Bindings.Add <| Binding(path, Mode = BindingMode.OneWay)

    binding.Converter <- {
        new IMultiValueConverter with

            member this.Convert(values, _, _, _) = 
                if Array.exists (fun x -> x = DependencyProperty.UnsetValue) values
                then 
                    DependencyProperty.UnsetValue
                else
                    try getter values.[0] 
                    with _ -> DependencyProperty.UnsetValue

            member this.ConvertBack(_, _, _, _) = raise <| NotImplementedException()
    }

    binding :> BindingBase

let (|DerivedProperty|_|) = function
    | PropertyGetterWithReflectedDefinition (Lambda (model, Lambda(unitVar, propertyBody))) as property when not property.CanWrite ->
        assert(unitVar.Type = typeof<unit>)
        getMultiBindingForDerivedProperty(model, propertyBody, property.GetValue) |> Some
    | _ -> None

let (|ExtensionDerivedProperty|_|) = function
    | MethodWithReflectedDefinition (Lambda (model, Lambda(unitVar, methodBody))) as getMethod when getMethod.Name.StartsWith(model.Type.Name + ".get_") -> 
        assert(unitVar.Type = typeof<unit>)
        let inline getter model = getMethod.Invoke(null, [| model |])
        getMultiBindingForDerivedProperty(model, methodBody, getter) |> Some
    | _ -> None

type DerivedPropertyAttribute = ReflectedDefinitionAttribute

let (|Target|_|) = function 
    | Some( FieldGet( Some( Value( window, _)), control)) -> 
        window |> control.GetValue |> unbox<DependencyObject> |> Some
    | _ -> None

let coerce _ = raise <| NotImplementedException()

let rec (|BindingInstance|_|) = function 
    | PropertyGet( Some( Value _), DerivedProperty binding, [])  
    | Call(None, ExtensionDerivedProperty binding, [ Value _ ]) -> Some binding
    | PropertyGet( Some( Value _), sourceProperty, []) -> 
        Some(upcast Binding(path = sourceProperty.Name))
    | SpecificCall <@ coerce @> (None, _, [ BindingInstance binding ]) -> 
        Some binding
    | NewObject( ctorInfo, [ BindingInstance binding ] ) 
        when ctorInfo.DeclaringType.GetGenericTypeDefinition() = typedefof<Nullable<_>> -> 
        Some binding 
    | SpecificCall <@ String.Format : string * obj -> string @> (None, [], [ Value(:? string as format, _); Coerce( BindingInstance binding, _) ]) ->
        binding.StringFormat <- format
        Some binding
    | Call(None, method', [ BindingInstance(:? Binding as binding) ]) -> 
        binding.Mode <- BindingMode.OneWay
        binding.Converter <- {
            new IValueConverter with
                member this.Convert(value, _, _, _) = 
                    try method'.Invoke(null, [| value |]) 
                    with _ -> DependencyProperty.UnsetValue
                member this.ConvertBack(_, _, _, _) = DependencyProperty.UnsetValue
        }
        Some( upcast binding)
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

