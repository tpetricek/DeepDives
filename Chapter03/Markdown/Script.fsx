// ======================================================================================
// Parsing spans using recursive functions 
// ======================================================================================


// --------------------------------------------------------------------------------------
// Listing 1. Representation of Markdown document 
// --------------------------------------------------------------------------------------

type MarkdownDocument = list<MarkdownBlock>

and MarkdownBlock = 
  | Heading of int * MarkdownSpans
  | Paragraph of MarkdownSpans
  | CodeBlock of list<string>

and MarkdownSpans = list<MarkdownSpan>

and MarkdownSpan =
  | Literal of string
  | InlineCode of string
  | Strong of MarkdownSpans
  | Emphasis of MarkdownSpans
  | HyperLink of MarkdownSpans * string
  | HardLineBreak

module Version1_Explicit =

  // ------------------------------------------------------------------------------------
  // Listing 2. Parsing the inline code span
  // ------------------------------------------------------------------------------------

  let rec parseInlineBody acc chars = 
    match chars with 
    | '`'::rest | ([] as rest) -> List.rev acc, rest
    | c::chars -> parseInlineBody (c::acc) chars
  let parseInline = function
    |'`'::chars -> Some(parseInlineBody [] chars)
    | _ -> None


  // Example: Running the parser
  "`code` and" |> List.ofSeq |> parseInline 

  // ------------------------------------------------------------------------------------
  // Listing 3. Parsing spans using function
  // ------------------------------------------------------------------------------------

  let toString chars =
    System.String(chars |> Array.ofList)

  let rec parseSpans acc chars = seq {
    let emitLiteral() = seq {
      if acc <> [] then 
        yield acc |> List.rev |> toString |> Literal }

    match parseInline chars, chars with
    | Some(body, chars), _ ->
        yield! emitLiteral ()
        yield body |> toString |> InlineCode
        yield! parseSpans [] chars
    | _, c::chars ->
        yield! parseSpans (c::acc) chars
    | _, [] ->
        yield! emitLiteral () }
    
  // Example: Running the parser      
  "`hello` world" |> List.ofSeq |> parseSpans []


// ======================================================================================
// Implementing parser using active patterns 
// ======================================================================================

module Version2_ActivePatterns =
  let toString chars =
    System.String(chars |> Array.ofList)

  // ------------------------------------------------------------------------------------
  // Listing 4. Implementing Delimited active pattern
  // ------------------------------------------------------------------------------------

  let (|StartsWith|_|) prefix list =
    let rec loop = function
      | [], rest -> Some(rest)
      | p::prefix, r::rest when p = r -> loop (prefix, rest)
      | _ -> None
    loop (prefix, list)

  let rec parseBracketedBody closing acc = function
    | StartsWith closing (rest) -> Some(List.rev acc, rest)
    | c::chars -> parseBracketedBody closing (c::acc) chars
    | _ -> None
  
  let (|Bracketed|_|) opening closing = function
    | StartsWith opening chars -> parseBracketedBody closing [] chars
    | _ -> None
  let (|Delimited|_|) delim = (|Bracketed|_|) delim delim
  
  // ------------------------------------------------------------------------------------
  // Listing 5. Parsing Markdown spans using active patterns 
  // ------------------------------------------------------------------------------------

  let rec parseSpans acc chars = seq {
    let emitLiteral() = seq {
      if acc <> [] then 
        yield acc |> List.rev |> toString |> Literal }

    match chars with
    | StartsWith [' '; ' '; '\n'; '\r'] chars
    | StartsWith [' '; ' '; '\n' ] chars
    | StartsWith [' '; ' '; '\r' ] chars -> 
        yield! emitLiteral ()
        yield HardLineBreak
        yield! parseSpans [] chars
    | Delimited ['`'] (body, chars) ->
        yield! emitLiteral ()
        yield InlineCode(toString body)
        yield! parseSpans [] chars
    | Delimited ['*'; '*' ] (body, chars)
    | Delimited ['_'; '_' ] (body, chars) ->
        yield! emitLiteral ()
        yield Strong(parseSpans [] body |> List.ofSeq)
        yield! parseSpans [] chars
    | Delimited ['*' ] (body, chars)
    | Delimited ['_' ] (body, chars) ->
        yield! emitLiteral ()
        yield Emphasis(parseSpans [] body |> List.ofSeq)
        yield! parseSpans [] chars
    | Bracketed ['['] [']'] (body, Bracketed ['('] [')'] (url, chars)) ->
        yield! emitLiteral ()
        yield HyperLink(parseSpans [] body |> List.ofSeq, toString url)
        yield! parseSpans [] chars
    | c::chars ->
        yield! parseSpans (c::acc) chars
    | [] ->
        yield! emitLiteral () }

  // Examples: Parsing single-paragraph Markdown texts

  "hello  \nworld  \n!!!" |> List.ofSeq |> parseSpans [] |> List.ofSeq    
  "**`hello` world** and _emph_" |> List.ofSeq |> parseSpans [] |> List.ofSeq

// ======================================================================================
// Parsing blocks using active pattern
// ======================================================================================

open Version2_ActivePatterns

// --------------------------------------------------------------------------------------
// Listing 6. Active patterns for parsing line-based synta
// --------------------------------------------------------------------------------------

module List = 
  let partitionWhile f = 
    let rec loop acc = function
      | x::xs when f x -> loop (x::acc) xs
      | xs -> List.rev acc, xs
    loop [] 

let (|LineSeparated|) lines =
  let isWhite = System.String.IsNullOrWhiteSpace
  match lines |> List.partitionWhile (isWhite >> not) with
  | par, _::rest | par, ([] as rest) -> par, rest
    
let (|AsCharList|) (str:string) = 
  str |> List.ofSeq

let (|PrefixedLines|) prefix (lines:list<string>) = 
  let prefixed, other = lines |> List.partitionWhile (fun line -> line.StartsWith(prefix))
  [ for line in prefixed -> line.Substring(prefix.Length) ], other

// Example: Testing the 'PrefixedLines' active pattern
let (PrefixedLines "..." res) = ["...1"; "...2"; "3" ]

// --------------------------------------------------------------------------------------
// Listing 7. Parsing Markdown blocks using active patterns 
// --------------------------------------------------------------------------------------

let rec parseBlocks lines = seq {
  match lines with
  | AsCharList(StartsWith ['#'; ' '] heading)::rest ->
      yield Heading(1, parseSpans [] heading |> List.ofSeq)
      yield! parseBlocks rest
  | AsCharList(StartsWith ['#'; '#'; ' '] heading)::rest ->
      yield Heading(2, parseSpans [] heading |> List.ofSeq)
      yield! parseBlocks rest
  | PrefixedLines "    " (body, rest) when body <> [] ->
      yield CodeBlock(body)
      yield! parseBlocks rest
  | LineSeparated (body, rest) when body <> [] -> 
      let body = String.concat " " body |> List.ofSeq
      yield Paragraph(parseSpans [] body |> List.ofSeq)
      yield! parseBlocks rest 
  | line::rest when System.String.IsNullOrWhiteSpace(line) ->
      yield! parseBlocks rest 
  | _ -> () }

// Example: Parsing a complete Markdown document!
let sample = """
  # Visual F#
  
  F# is a **programming language** that supports _functional_, as       
  well as _object-oriented_ and _imperative_ programming styles.        
  Hello world can be written as follows:                                

      printfn "Hello world!"                                            

  For more information, see the [F# home page] (http://fsharp.net) or 
  read [Real-World Func tional Programming](http://manning.com/petricek) 
  published by [Manning](http://manning.com).

"""
let doc = parseBlocks (sample.Split('\r', '\n') |> List.ofSeq) |> List.ofSeq

// ======================================================================================
// Turning Markdown into HTML 
// ======================================================================================

open System.IO

// Helper function that generates a simple HTML element
let outputElement (output:TextWriter) (tag:string) attributes body =
  let attrString = String.concat " " [ for k, v in attributes -> k + "=\"" + v + "\"" ]
  output.Write("<" + tag + attrString + ">")
  body () 
  output.Write("</" + tag + ">")

// --------------------------------------------------------------------------------------
// Listing 8. Generating HTML from a Markdown document
// --------------------------------------------------------------------------------------

let rec formatSpan (output:TextWriter) = function
  | Literal(str) -> output.Write(str)
  | InlineCode(code) -> output.Write("<code>" + code + "</code>")
  | Strong(spans) -> 
      outputElement output "strong" [] (fun () -> 
        spans |> List.iter (formatSpan output))
  | Emphasis(spans) ->
      outputElement output "em" [] (fun () ->
        spans |> List.iter (formatSpan output))
  | HyperLink(spans, url) ->
      outputElement output "a" ["href", url] (fun () ->
        spans |> List.iter (formatSpan output))
  | HardLineBreak -> 
      output.Write("<br />") // Exercise!

let rec formatBlock (output:TextWriter) = function
  | Heading(size, spans) ->
      outputElement output ("h" + size.ToString()) [] (fun () ->
        spans |> List.iter (formatSpan output))
  | Paragraph(spans) ->
      outputElement output "p" [] (fun () ->
        spans |> List.iter (formatSpan output))
  | CodeBlock(lines) ->
      outputElement output "pre" [] (fun () ->
        lines |> List.iter output.WriteLine )

// Example: Running the HTML formatter on sample document
let sb = System.Text.StringBuilder()
let output = new StringWriter(sb)
doc |> Seq.iter (formatBlock output)
sb.ToString()

// ======================================================================================
// Processing Markdown documents
// ======================================================================================

// --------------------------------------------------------------------------------------
// Listing 9. Acive patterns that simplify processing of Markdown documents
// --------------------------------------------------------------------------------------

module Matching =
  let (|SpanNode|_|) span = 
    match span with
    | Strong spans | Emphasis spans | HyperLink(spans, _) -> 
        Some(box span, spans)
    | _ -> None

  let SpanNode (span:obj, spans) =
    match unbox span with
    | Strong _ -> Strong spans 
    | Emphasis _ -> Emphasis spans
    | HyperLink(_, url) -> HyperLink(spans, url)
    | _ -> invalidArg "" "Incorrect MarkdownSpan"

  let (|BlockSpans|_|) block =
    match block with  
    | Heading(_, spans)
    | Paragraph(spans) -> Some(box block, spans)
    | _ -> None

  let BlockSpans (block:obj, spans) = 
    match unbox block with 
    | Heading(a, _) -> Heading(a, spans)
    | Paragraph(_) -> Paragraph(spans)
    | _ -> invalidArg "" "Incorrect MarkdownBlock."

// Example: Counting words in the document
let rec countSpanWords = function
  | Literal str -> 
      str.Split([| ','; '.'; '!'; ' '; '\n'; '\r' |], System.StringSplitOptions.RemoveEmptyEntries).Length
  | Matching.SpanNode(_, spans) -> 
      spans |> List.sumBy countSpanWords
  | _ -> 0

let countBlockWords = function
  | Matching.BlockSpans(_, spans) -> spans |> List.sumBy countSpanWords
  | _ -> 0

List.sumBy countBlockWords doc

// --------------------------------------------------------------------------------------
// Listing 10. Generating a document with references for printing 
// --------------------------------------------------------------------------------------

let rec generateSpanReferences (refs:ResizeArray<_>) = function
  | HyperLink(body, url) -> 
      let id = sprintf "[%d]" (refs.Count + 1)
      refs.Add(id, url)
      [HyperLink(body, url); Literal(id)]
  | Matching.SpanNode(shape, spans) ->
      let spans = spans |> List.collect (generateSpanReferences refs)
      [Matching.SpanNode(shape, spans)]
  | span -> [span]

let generateBlockReferences refs = function
  | Matching.BlockSpans(shape, spans) ->
      let spans = spans |> List.collect (generateSpanReferences refs)
      Matching.BlockSpans(shape, spans)
  | block -> block

// Example: Generating references from a sample document
let ndoc = parseBlocks [ """For more information, see the 
  [F# home page](http://fsharp.net) or read [Real-World Functional 
  Programming](http://manning.com/petricek) published by 
  [Manning](http://manning.com).""" ] |> List.ofSeq
let refs = ResizeArray<_>()
let docRef = ndoc |> List.map (generateBlockReferences refs)
