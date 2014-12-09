namespace ThePeopleWhoChat.Core

    open System

    type LineParser(sepTest:char -> bool, quotTest:char -> bool,
                    escTest:char * char -> bool,escRepl:char * char -> char) =

        static let isComma(c) = c = ','
        static let isSpace(c) = Char.IsWhiteSpace(c)
        static let isQuot(c) = c = '\'' || c = '\"'
        static let isEscQuot(c,d) = (c = '\"' && d = '\"') || (c = '\'' && d = '\'')

        static member CommaSeperatedParser = 
            LineParser( isComma, isQuot, isEscQuot, fst)
        static member SpaceSeperatedParser = 
            LineParser( isSpace, isQuot, isEscQuot, fst)

        member x.Parse(line:string) =

            let rec parse (underway:char list,inquotes:bool) (remain:char list) = 
                seq {
                    let getString(cl:char list) =
                        new System.String(cl |> Array.ofList |> Array.rev)
                    match remain,underway,inquotes with
                    | q::[],y,true when quotTest(q) 
                                        -> yield getString(y)
                    | [],y,_            -> yield getString(y)
                    | q::w::x,y,z when escTest(q,w) 
                                        -> yield! x |> parse ((escRepl(q,w))::y, z)
                    | c::q::w::x,y,false 
                        when sepTest(c) && quotTest(q) && not(escTest(q,w))
                                        -> yield getString(y)
                                           yield! (w::x) |> parse ([],true)
                    | q::c::x,y,true when sepTest(c) && quotTest(q) 
                                        -> yield! (c::x) |> parse (y,false)
                    | c::x,y,true when sepTest(c) 
                                        -> yield! x |> parse (c::y, true)
                    | c::x,y,false when sepTest(c) 
                                        -> yield getString(y)
                                           yield! x |> parse ([], false)
                    | c::x,y,z          -> yield! x |> parse (c::y, z)
                    }
            match(line.ToCharArray() |> List.ofArray) with
            | [] -> Seq.empty
            | q::w::x when quotTest(q) && not(escTest(q,w)) 
                                    -> (w::x) |> parse ([],true)
            | x -> x |> parse ([],false)