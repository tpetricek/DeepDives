// ----------------------------------------------------------------------------
// 1.1 Starting with expressions
// ----------------------------------------------------------------------------

// Famous hello world sample in F#
printfn "Hello world"

// ----------------------------------------------------------------------------
// Listing 1. Sequencing expressions
// ----------------------------------------------------------------------------

// Using ";" operator for sequencing
let answer1 = printf "Thinking deeply..."; 42

// Using indentation for sequencing
let answer2 = 
  printf "Thinking deeply..."
  42

// ----------------------------------------------------------------------------
// Listing 2. Writing conditional expressions
// ----------------------------------------------------------------------------

let lo = 10
let hi = 5
if lo > hi then 
  "Wrong"
"Good"

if lo > hi then "Wrong" else "Good"

// ----------------------------------------------------------------------------
// Listing 3. Comparing expressions and functions
// ----------------------------------------------------------------------------

let helloResult = 
  printfn "Hello world!"

let sayHello () = 
  printfn "Hello world!"

// Try running the following lines
helloResult
helloResult

sayHello()
sayHello()

// ----------------------------------------------------------------------------
// Section 1.3 Using functions as values
// ----------------------------------------------------------------------------

let sayEverything printer words = 
  for word in words do
    printer word

// Using anonymous function
sayEverything (fun num -> printfn "%d" num) [1; 2; 3] 

// Using partial function application
sayEverything (printfn "%s") ["hello"; "world"]

// Using pipelining operator
["hello"; "world"] |> sayEverything (fun s ->
  printfn "%s" (s.ToUpper()))

// ----------------------------------------------------------------------------
// 2.1 Representing composite values
// ----------------------------------------------------------------------------

let person = ("Alexander", 3)

// ----------------------------------------------------------------------------
// Listing 4. Incrementing value in a tuple
// ----------------------------------------------------------------------------

let incAge person = 
  let name, age = person
  name, age + 1

// Extract just age
let _, age = person

// Fails when age is not 3
let name, 3 = person

// Using tuples as arguments
incAge person
incAge ("Tomas", 29)

// Shorter version
let incAgeShort (name, age) = 
  name, age + 1

// ----------------------------------------------------------------------------
// Listing 5. Record with custom ToString method
// ----------------------------------------------------------------------------

type Person = 
  { Name : string
    Age : int }
  override x.ToString() = 
    sprintf "%s (%d)" x.Name x.Age

// Working with records

let incAgeRec person = 
  { person with Age = person.Age + 1 }

let jan = { Name = "Jan"; Age = 24 }
incAgeRec jan

// ----------------------------------------------------------------------------
// Listing 6. Domain model for customers with address
// ----------------------------------------------------------------------------
module Listing6 = 

  type Address =
    | NotSet
    | Local of street:string * postcode:string
    | International of country:string

  type Customer = 
    { Name : string 
      Shipping : Address }

  // --------------------------------------------------------------------------
  // Listing 7. Calculating shipping price
  // --------------------------------------------------------------------------

  let shippingPrice address = 
    match address with
    | NotSet -> invalidOp "Address not set!"
    | Local(postcode=p) when p.StartsWith("NY") -> 0.0M
    | Local _ -> 5.0M
    | International("Canada") -> 20.0M
    | International _ -> 25.0M

  let cz = International "Czech Republic"
  let customer = { Name = "Tomas"; Shipping = cz }
  shippingPrice customer.Shipping

  shippingPrice null

// ----------------------------------------------------------------------------
// Listing 8. Better domain model
// ----------------------------------------------------------------------------

type Address =
  | Local of street:string * postcode:string
  | International of country:string

type Customer = 
  { Name : string 
    Shipping : Address option }

let shippingPrice address = 
  match address with
  | Local(postcode=p) when p.StartsWith("NY") -> 0.0M
  | Local _ -> 5.0M
  | International("Canada") -> 20.0M
  | International _ -> 25.0M


// Safe handling of missing values
// (using pattern matching on options)

let customer = { Name = "Jan"; Shipping = None }
match customer.Shipping with
| Some addr -> 
    printfn "Price: %A" (shippingPrice addr)
| _ -> 
    printfn "Enter address!"

// ----------------------------------------------------------------------------
// Section 3. Object-oriented programming, the good parts
// ----------------------------------------------------------------------------

type CustomerValidator = Customer -> bool

type ICustomerValidator =
  abstract Description : string
  abstract Validate : Customer -> bool

// ----------------------------------------------------------------------------
// Listing 9. Validating address using object expressions
// ----------------------------------------------------------------------------

let hasAddress = 
  { new ICustomerValidator with
      member x.Description =
        "Address is required for shipment"
      member x.Validate(cust) =
        cust.Shipping.IsSome }

// ----------------------------------------------------------------------------
// Listing 10. Validating name using object types
// ----------------------------------------------------------------------------

type NameLength() =
  member val RequiredLength = 10 with get, set
  interface ICustomerValidator with
    member x.Description = 
      "The name should not be empty"
    member x.Validate(cust) = 
      cust.Name <> null && 
      cust.Name.Length >= x.RequiredLength

// ----------------------------------------------------------------------------
// Listing 11. Programming with objects interactively
// ----------------------------------------------------------------------------

let v1 = hasAddress
let v2 = NameLength(RequiredLength=5)
let validators = [ v1; v2 :> ICustomerValidator ]

let cust = { Name = "Tomas"; Shipping = None }
for v in validators do
  if not (v.Validate(cust)) then 
    printfn "%s" v.Description

// ----------------------------------------------------------------------------
// Listing 12. Composing validators using object expressions
// ----------------------------------------------------------------------------

let composeValidators (validators:seq<ICustomerValidator>) = 
  { new ICustomerValidator with
      member x.Description =
        [ for v in validators -> v.Description ]
        |> String.concat "\n"
      member x.Validate(cust) =
        validators |> Seq.forall (fun v -> v.Validate(cust)) }

// ----------------------------------------------------------------------------
// Sidebar: Implementing interface by expression
// ----------------------------------------------------------------------------

type Composed() = 
  let hasSpace = 
    { new ICustomerValidator with
        member x.Description = "Name contains space"
        member x.Validate(cust) = cust.Name.Contains(" ") }
  let nameLength = NameLength()
  let composed = composeValidators [hasSpace; nameLength ]
  member x.Strict
    with get() = nameLength.RequiredLength = 10
    and set(v) = nameLength.RequiredLength <- if v then 10 else 5

  // Proposed for future versions of F#:
  //   interface ICustomValidator = composed
  
