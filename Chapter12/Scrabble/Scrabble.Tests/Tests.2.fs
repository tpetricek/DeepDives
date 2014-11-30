#if INTERACTIVE
#load "../ScrabbleLibrary/ScrabbleModule.fs"
#I "../packages/NUnit.2.6.3/lib"
#I "../packages/FsUnit.1.2.1.0/Lib/Net40"
#I "../packages/FsCheck.0.9.3.0/lib/net40-Client"
#I "../packages/Unquote.2.2.2/lib/net40"
#r "nunit.framework.dll"
#r "FsUnit.NUnit.dll"
#r "FsUnit.NUnit.dll"
#r "FsCheck.dll"
#r "Unquote.dll"
#else 
module Scrabble.Tests
#endif


// ----------------------------------------------------------------------------
// Section 12.2.2 - Writing tests using module and functions
// ----------------------------------------------------------------------------

open NUnit.Framework
open Scrabble

let [<Test>] ``Value of QUARTZ is 24`` () =         
    Assert.AreEqual(wordPoints "QUARTZ", 24)

// ----------------------------------------------------------------------------
// Section 12.2.2 - Writing fluent assertions using FsUnit
// ----------------------------------------------------------------------------

open FsUnit
open System

let [<Test>] ``Value of QUARTZ is 24 (using FsUnit)`` () =         
    wordPoints "QUARTZ" |> should equal 24

let [<Test>] ``Calculating value of "Hello" should throw`` () =         
    TestDelegate(fun () -> wordPoints "Hello" |> ignore) 
    |> should throw typeof<InvalidOperationException>

// ----------------------------------------------------------------------------
// Section 12.2.2 - Writing assertions using Unquote
// ----------------------------------------------------------------------------

open Swensen.Unquote

let [<Test>] ``Value of QUARTZ is 24 (using Unquote)`` () =
    test <@ wordPoints "QUARTZ" = 24 @>

// ----------------------------------------------------------------------------
// 12.2.3 Parameterized tests 
// ----------------------------------------------------------------------------

// Demo 1 - Using the 'Values' attribute

let [<Test>] ``Value of B,C,M,P should be 3`` 
    ([<Values('B','C','M','P')>] letter:char) = 
        Assert.AreEqual(letterPoints letter, 3)

// Demo 2 - Using the 'TestCase' attribute

[<TestCase("HELLO",Result=8)>]
[<TestCase("QUARTZ",Result=24)>]
[<TestCase("FSHARP",Result=14)>]
let ``Sample word has the expected value`` (word:string) =
    wordPoints word

// Demo 3 - Writing combinatorial tests

[<Test; Combinatorial>]
let ``Word value is not zero or minus one`` 
      ( [<Values(Int32.MinValue, -1, 0, Int32.MaxValue)>] value:int, 
        [<Values("HELLO","FSHARP")>] word:string) =
    Assert.AreNotEqual(wordPoints word, value)

// ----------------------------------------------------------------------------
// Listing 12.4 – Specifying Scrabble properties
// ----------------------------------------------------------------------------

open FsCheck

let repetitionsMultiply (repetitions:uint8) =
  let s = (String(Array.create (int repetitions) 'C'))
  wordPoints s = (int repetitions) * letterPoints 'C'

let letterPositive (letter:char) =
  letterPoints letter > 0 

Check.Quick(repetitionsMultiply)
Check.Quick(letterPositive)

// ----------------------------------------------------------------------------
// Listing 12.5 - Wrapping properties as unit tests
// ----------------------------------------------------------------------------

[<Test>]
let ``Value of a valid letter is positive`` () =
  Check.Quick(fun (letter:char) ->
    (letter >= 'A' && letter <= 'Z') ==> lazy (letterPoints letter > 0))

[<Test>]
let ``Score of a repeated letter is multiple of its value`` () =
  Check.Quick(fun (letter:char) (repetitions:uint8) ->
    let s = (String(Array.create (int repetitions) letter))
    (letter >= 'A' && letter <= 'Z') ==> 
      lazy (wordPoints s = (int repetitions) * letterPoints letter)
