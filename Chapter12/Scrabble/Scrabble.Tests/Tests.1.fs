// ----------------------------------------------------------------------------
// Listing 12.2 Unit tests for Scrabble, using classes and members
// ----------------------------------------------------------------------------

namespace Scrabble.Tests1

open NUnit.Framework
open Scrabble

[<TestFixture>]                                        
type ScrabbleTests() = 
    [<Test>]                                           
    member test.``Value of QUARTZ is 24`` () =         
        Assert.AreEqual(wordPoints "QUARTZ", 24)
