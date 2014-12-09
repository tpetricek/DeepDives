namespace DeepDives.Provider

type InferedType =
  | TopType
  | StringType
  | DateType
  | NumericType of isDecimal:bool

open System

module Inference = 

  let inferStringType str =
      if fst (Int32.TryParse str) then NumericType(false)
      elif fst (Double.TryParse str) then NumericType(true)
      elif fst (DateTime.TryParse str) then DateType
      else StringType
    
  let generalizeTypes = function
    | t1, t2 when t1 = t2 -> t1
    | TopType, t | t, TopType -> t
    | StringType, _ | _, StringType -> StringType
    | DateType, _ | _, DateType -> StringType
    | NumericType(f1), NumericType(f2) -> 
        NumericType(f1 || f2)

open FSharp.Data
open Inference

type CsvFile<'T>(parser:(CsvRow -> 'T), file:string) =
  let data = CsvFile.Load(file)  
  member x.Headers = data.Headers
  member x.Rows = 
    seq { for row in data.Rows -> parser row }

module Provider = 
  let getTypeAndParser = function
    | NumericType(false) -> typeof<int>, fun e -> <@@ Int32.Parse(%%e) @@>
    | NumericType(true) -> typeof<float>, fun e -> <@@ Double.Parse(%%e) @@>
    | DateType -> typeof<DateTime>, fun e -> <@@ System.DateTime.Parse(%%e) @@>
    | StringType | TopType -> typeof<string>, fun e -> e 

  open ProviderImplementation.ProvidedTypes
  open Microsoft.FSharp.Quotations

  // get the index, name, type, conversion, and location of each field
  //let sample = CsvFile.Load("sample.csv")

  let generateTypesAndBuilder (sample:CsvFile) =
    let rows = sample.Rows |> Seq.toArray
    let fieldInfos = 
      [ for i in 0 .. sample.NumberOfColumns - 1 do
          let header = sample.Headers.Value.[i]
          let ty =
              [ for row in rows |> Seq.take 10 -> inferStringType row.Columns.[i] ]
              |> Seq.fold (fun t1 t2 -> generalizeTypes(t1, t2)) TopType
          yield i, header, (getTypeAndParser ty) ]

    let elementTypes = [| for (_, _, (ty, _)) in fieldInfos -> ty |]
    let tupleTy = Reflection.FSharpType.MakeTupleType(elementTypes)
      
    let rowTy = ProvidedTypeDefinition("Row", Some(tupleTy))
    for i, fieldName, (fieldTy, _) in fieldInfos do
        let prop = ProvidedProperty(fieldName, fieldTy)
        prop.GetterCode <- fun [row] -> Quotations.Expr.TupleGet(row, i)
        rowTy.AddMember(prop)

    let strs = Quotations.Var("strs", typeof<CsvRow>)
    let strsVar = Expr.Var(strs)
    let convertedItems =
      [ for i, _, (ty, parser) in fieldInfos do
          let index = Expr.Value(i)
          yield parser <@@ (%%strsVar:CsvRow).Columns.[%%index] @@> ]

    // then build a tuple out of them
    let tup = Quotations.Expr.NewTuple(convertedItems)
    let builder = Quotations.Expr.Lambda(strs, tup)

    rowTy, tupleTy, builder 

open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations

module Hack = 
  do AppDomain.CurrentDomain.add_AssemblyResolve(fun e r -> 
    if r.Name.StartsWith("FSharp.Data") then typeof<CsvRow>.Assembly
    else null)

[<TypeProvider>]
type public TescoProvider(cfg:TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces()

  // Boilerplate that generates root type in current assembly
  let asm = System.Reflection.Assembly.GetExecutingAssembly()
  let ns = "DeepDives"
  let csvProvider = ProvidedTypeDefinition(asm, ns, "CsvProvider", Some(typeof<obj>))

  // Add static parameters that specify the credentials
  let sampleParam = ProvidedStaticParameter("sample", typeof<string>)
  do csvProvider.DefineStaticParameters([sampleParam], fun typeName [| sample |] ->
  
    let rowTy, erasedRowTy, convFunc = Provider.generateTypesAndBuilder (CsvFile.Load(sample :?> string))
    let ty = typedefof<CsvFile<_>>.MakeGenericType(erasedRowTy)
    let topTy = ProvidedTypeDefinition(asm, ns, typeName, Some ty, HideObjectMethods = true)
    topTy.AddMember(rowTy)

    let pp = ProvidedProperty("Rows", typedefof<seq<_>>.MakeGenericType(rowTy))
    pp.GetterCode <- fun [self] -> Expr.PropertyGet(self, ty.GetProperty("Rows"))
    topTy.AddMember(pp)

    let pc = ProvidedConstructor [ ProvidedParameter("file", typeof<string>) ]
    pc.InvokeCode <- fun [file] -> Expr.NewObject(ty.GetConstructors() |> Seq.head, [convFunc; file])
    topTy.AddMember(pc)

    topTy )
 
  // Register the main (parameterized) type with F# compiler
  do this.AddNamespace(ns, [ csvProvider ])

// ---------------------------------------------------------------------------------------------
// #r @"..\packages\FSharp.Data.2.0.0-beta3\lib\net40\FSharp.Data.dll"
// open FSharp.Data

module YahooRuntime = 
  type Sectors = XmlProvider< @"http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.sectors&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys" >
  for s in Sectors.GetSample().Results.Sectors do
    printfn "%A" s.Name
    for sub in s.Industries do
      printfn "(%d) %s" sub.Id sub.Name

  let http = Http.RequestString(@"http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.sectors&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys")

  let yahooIndustries = Lazy.Create(fun () -> 
    [ for sector in Sectors.GetSample().Results.Sectors ->
        let industries = [ for sub in sector.Industries -> sub.Id, sub.Name ]
        sector.Name, industries ] )

type Companies = XmlProvider< @"http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.industry%20where%20id%3D%22852%22&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys" >

[<TypeProvider>]
type public YahooProvider(cfg:TypeProviderConfig) as this =
  inherit TypeProviderForNamespaces()

  // Boilerplate that generates root type in current assembly
  let asm = System.Reflection.Assembly.GetExecutingAssembly()
  let ns = "DeepDives"

  let generateIndustry id name (rowTy:Type) (erasedTyp:Type) convFunc = 
    let p = ProvidedTypeDefinition(name, None)
    p.AddMembersDelayed(fun () ->
        let companies = Companies.Load(@"http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.industry%20where%20id%3D%22" + (string id) + @"%22&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys")
        let csvRuntimeTyp = typedefof<CsvFile<_>>.MakeGenericType(erasedTyp)
        let csvCtor = csvRuntimeTyp.GetConstructors() |> Seq.head
        [ for c in companies.Results.Industry.Companies ->

            let file = Expr.Value("http://ichart.yahoo.com/table.csv?s=" + c.Symbol)
            let body = Expr.PropertyGet(Expr.NewObject(csvCtor, [convFunc; file]), csvRuntimeTyp.GetProperty("Rows"))

            let companyTy = ProvidedProperty(c.Name, typedefof<seq<_>>.MakeGenericType(rowTy), IsStatic = true)
            companyTy.GetterCode <- fun _ -> body
            companyTy  ] )            
    p

  let generateType ()=
    let yahooTyp = ProvidedTypeDefinition(asm, ns, "Yahoo", None)
    let sample = CsvFile.Load("http://ichart.yahoo.com/table.csv?s=FB")
    let rowTy, erasedTy, convFunc = Provider.generateTypesAndBuilder sample 
    yahooTyp.AddMember(rowTy)

    for sector, industries in YahooRuntime.yahooIndustries.Value do

      let sectorTyp = ProvidedTypeDefinition(sector, Some typeof<obj>)
      yahooTyp.AddMember(sectorTyp)

      for id, name in industries do
        let industryTyp = generateIndustry id name rowTy erasedTy convFunc
        sectorTyp.AddMember(industryTyp)

    yahooTyp

  do this.AddNamespace(ns, [ generateType() ])

(*
industryTy.AddMembersDelayed(fun () ->
    let xmlDoc = System.Xml.XmlDocument()
                    
    ("http://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.industry%20where%20id%3D%22" + id + "%22& env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys")
    |> System.Net.WebClient().OpenRead
    |> xmlDoc.Load

    let companyCol = xmlDoc.CreateNavigator().Select("query/results/industry/company")
                    
    [while companyCol.MoveNext() do
        let company = companyCol.Current
        let name = company.GetAttribute("name", "")
        let symbol = company.GetAttribute("symbol", "")
        let companyTy = ProvidedTypeDefinition(name, None)
        yield companyTy :> MemberInfo
        ...]

companyTy.AddMember(
    let startParam = ProvidedParameter("startDate", typeof<DateTime>)
    let endParam = ProvidedParameter("endDate", typeof<DateTime>)
    let periodParam = ProvidedParameter("endDate", typeof<Period>)
    ProvidedMethod("GetData", [startParam; endParam; periodParam], csvTy,
        InvokeCode = 
            fun [_;startDate;endDate;period] -> 
                <@@ let startDate : DateTime = %%startDate
                    let endDate : DateTime = %%endDate
                    let period = 
                        match %%period with
                        | Period.Day -> 'd'
                        | Period.Month -> 'm'
                        | Period.Week -> 'w'
                    let url = sprintf "http://ichart.yahoo.com/table.csv?s=%s&a=%i&b=%i&c=%i&d=%i&e=%i&f=%i&g=%c&ignore=.csv" symbol (startDate.Month-1) startDate.Day startDate.Year (endDate.Month-1) endDate.Day endDate.Year period
                    Samples.Csv.Runtime.CsvFileImpl(Uri(url), ',', '"', false, fun strs -> DateTime.Parse strs.[0], double strs.[1], double strs.[2], double strs.[3], double strs.[4], int strs.[5], double strs.[6]) @@>))])
  
  *)
[<assembly:TypeProviderAssembly>]
do()