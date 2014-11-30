namespace FSharpDeepDives

module Schedule = 
    
    open System

    type CronFieldType = 
        | Value of string
        | Range of string * string
        | Increment of string * string
        | List of CronFieldType list
        | Every
        | Ignore
  
    type CronExpr = CronFieldType list
    
    let private monthIndex = function
        | "JAN" -> 1  
        | "FEB" -> 2  
        | "MAR" -> 3  
        | "APR" -> 4  
        | "MAY" -> 5  
        | "JUN" -> 6  
        | "JUL" -> 7  
        | "AUG" -> 8  
        | "SEP" -> 9  
        | "OCT" -> 10 
        | "NOV" -> 11 
        | "DEC" -> 12
        | a when a.ToCharArray() |> Seq.forall(Char.IsDigit) -> Int32.Parse(a)
        | a -> failwith "Unrecognised month accepted values are JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV, DEC or numeric values 1 through 12"

    let private dayOfWeekIndex = function
        | "SUN" -> 0
        | "MON" -> 1
        | "TUE" -> 2
        | "WED" -> 3
        | "THU" -> 4
        | "FRI" -> 5
        | "SAT" -> 6
        | a when a.ToCharArray() |> Seq.forall(Char.IsDigit) -> Int32.Parse(a) - 1
        | _ -> failwith "Unrecognised week day accepted values are SUN, MON, TUE, WED, THU, FRI, SAT or numeric values 1 through 7"


    let parse (str : string) =    
        let rec _parse' (str : string) (state : _ list) =
             match str.ToCharArray() |> Seq.toList with
             | '?' :: [] when state.Length = 1 || state.Length = 3 -> Ignore :: state
             | '?' :: [] -> failwithf "? can only be specified on Day Of Month or Day of week fields %A" state.Length
             | '*' :: [] -> Every :: state
             | _  when str.Contains("/") ->
                  match str.Split([|'/'|]) |> Seq.toList with
                  | n :: d :: [] -> 
                        Increment(n,d) :: state
                  | _ -> failwithf "Unable to parse %A to an increment, e.g. 0/5" str
             | _  when str.Contains(",") ->
                  List(str.Split([|','|]) |> List.ofArray |> List.collect (fun x -> (_parse' x []))) :: state
             | _  when str.Contains("-") -> 
                  match str.Split([|'-'|]) |> Seq.toList with
                  | n :: d :: [] -> Range(n,d) :: state
                  | _ as a -> failwithf "Unable to parse %A to an Range, e.g. 0-5" str
             | [] -> state
             
             | _ -> Value(str) :: state
            
        match Array.foldBack (_parse') (str.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)) [] with
        | secs :: mins :: hrs :: day :: mnth :: dayofwk :: year :: [] ->
            [secs; mins; hrs; day; mnth; dayofwk; year]
        | _ -> failwith "Invalid cron expression valid cron expressions take the form (* * * * * * * => sec min hour day month dayofweek year)"

    let rec getValuesForField maxValue minValue mapF = function
            | Ignore -> Seq.empty
            | Every -> seq { for i in minValue .. maxValue do yield i }
            | Increment(s, i) -> 
                let start = s |> mapF
                let inc = i |> mapF
                seq { for x in start .. inc .. maxValue do yield x }           
            | Range(s,e) -> seq { for i in s |> mapF .. e |> mapF do yield i }
            | Value(v) -> Seq.singleton (v |> mapF)
            | List(fields) ->
                    fields
                    |> Seq.fold (fun s f -> Seq.append (getValuesForField maxValue minValue mapF f) s) Seq.empty
                    |> Seq.distinct
                    |> Seq.toList |> List.rev |> Seq.ofList |> Seq.sort

    let toSeqAsOf (startDate : DateTime) (cron : string) = 
        let expr = parse cron
        let years = 
            getValuesForField 9999 startDate.Year (Int32.Parse) expr.[6]
            |> Seq.cache
        let daysofweek =
            getValuesForField 7 1 dayOfWeekIndex expr.[5]
            |> Seq.map (fun dw -> DayOfWeek.ToObject(typeof<DayOfWeek>,  dw) :?> DayOfWeek)
            |> Seq.cache
        let months = getValuesForField 12 1 monthIndex expr.[4] |> Seq.cache
        let days = 
            if expr.[3] = Ignore then Every else expr.[3]
            |> getValuesForField 31 1 (Int32.Parse) 
            |> Seq.cache
        let hours = getValuesForField 23 0 (Int32.Parse) expr.[2] |> Seq.cache
        let mins = getValuesForField 59 0 (Int32.Parse) expr.[1] |> Seq.cache
        let secs = getValuesForField 59 0 (Int32.Parse) expr.[0] |> Seq.cache
        
        let dowPred =
            if expr.[5] = Ignore 
            then (fun _ -> true)
            else (fun (dt : DateTime) -> Seq.exists (fun x -> dt.DayOfWeek = x) daysofweek)
                                  
        seq {
            for yr in years do
             for mth in months do
              for day in days |> Seq.filter (fun x -> x <= (DateTime.DaysInMonth(yr, mth))) do
               for hour in hours do
                for min in mins do
                 for sec in secs do
                     let dt = DateTime(yr, mth, day, hour, min, sec)
                     if (dt.Ticks >= startDate.Ticks)
                     then yield dt 
        } |> Seq.filter dowPred

    let toSeq (cron : string) = 
        toSeqAsOf DateTime.Now cron

    let toAsync cron job = 
        async { 
                let enum = (toSeq cron).GetEnumerator()
                while enum.MoveNext() do 
                    let diff = enum.Current.Subtract(DateTime.Now)
                    if diff.TotalMilliseconds >= 0. then
                        do! Async.Sleep(diff.TotalMilliseconds |> int)
                        do! job
       }
    
    let toObservable (cron : string) =
      let observers = ref []
      let cts = new Threading.CancellationTokenSource()

      let notifyObservers f =
          !observers
          |> Seq.map (fun (observer:IObserver<_>) -> async { return f observer})
          |> Async.Parallel
          |> Async.RunSynchronously
          |> ignore
      
      let next value =
       notifyObservers (fun observer -> observer.OnNext value)
 
      let error error =
        notifyObservers (fun observer -> observer.OnError error)
        observers := []
 
      let completed()=
        notifyObservers (fun observer -> observer.OnCompleted())
        observers := []  
      
      let worker = 
          Async.Start(async { 
                let enum = (toSeq cron).GetEnumerator()
                while enum.MoveNext() && not <| cts.IsCancellationRequested do 
                    let diff = enum.Current.Subtract(DateTime.Now)
                    if diff.TotalMilliseconds >= 0. then
                        do! Async.Sleep(diff.TotalMilliseconds |> int)
                        do next enum.Current
            }, cts.Token)

      { new IObservable<DateTime> with
         member o.Subscribe(observer)  =
           observers := observer :: !observers
           {new IDisposable with
              member this.Dispose() =
                 observers := !observers |> List.filter ((<>) observer)
           }
      }
 
