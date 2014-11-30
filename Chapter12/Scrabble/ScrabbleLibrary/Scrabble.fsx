// ----------------------------------------------------------------------------
// Listing 12.1 - Calculating word score in Scrabble
// ----------------------------------------------------------------------------

let letterPoints = function
    | 'A' | 'E' | 'I' | 'L' | 'N' | 'O' | 'R' | 'S' | 'T' | 'U' -> 1
    | 'D' | 'G' -> 2
    | 'B' | 'C' | 'M' | 'P' -> 3
    | 'F' | 'H' | 'V' | 'W' | 'Y' -> 4
    | 'K' -> 5
    | 'J' | 'X' -> 8
    | 'Q' | 'Z' -> 10
    | a -> invalidOp <| sprintf "Letter %c" a

let wordPoints (word:string) =
    word |> Seq.sumBy letterPoints

// Exploratory testing: Calling functions with sample inputs

wordPoints "QUARTZ"
wordPoints "Hello"

// ----------------------------------------------------------------------------
// Checking non-functional requirements
// ----------------------------------------------------------------------------

let s = System.String(Array.create 10000000 'Z')
wordPoints s

// Alternative version of the function that is

let wordPointsMutable (word:string) =
  let mutable sum = 0
  for i = 0 to word.Length - 1 do
    sum <- sum + letterPoints(word.[i])
  sum

wordPointsMutable s