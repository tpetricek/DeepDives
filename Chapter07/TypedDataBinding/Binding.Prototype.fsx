#r "PresentationCore"
#r "PresentationFramework"
#r "WindowsBase"
#r "System.Xaml"

//Listing 12 Parsing the assignment statement to the data binding expression
module Binding = 

    open System.Reflection
    open System.Windows
    open Microsoft.FSharp.Quotations.Patterns

    let ofExpression = function
        | PropertySet
            (
//Control (binding target) from left left-hand side of assignment  
                Some( FieldGet( Some( Value( window, _)), control)),
//Property from left left-hand side of assignment                    
                targetProperty,                                          
                [], 
//Right-hand side has reference to model (binding source) and property (binding path)
                PropertyGet( Some( Value _), sourceProperty, [])           
            ) ->
                let target : FrameworkElement = 
                    window |> control.GetValue |> unbox 
//Following WPF convention, dependency property name is formed by adding suffix “Property” to regular companion .NET property.           
                let dpPropertyName = targetProperty.Name + "Property" 
                let dp = 
                    targetProperty
                        .DeclaringType
                        .GetField(dpPropertyName)
                        .GetValue(null, [||]) 
                        |> unbox<DependencyProperty> 

                target.SetBinding(dp, path = sourceProperty.Name) |> ignore
//Single case active pattern. Fails at run-time if cannot can’t be matched. 
        | expr -> failwithf "Invalid binding quotation:\n%O" expr       
            

