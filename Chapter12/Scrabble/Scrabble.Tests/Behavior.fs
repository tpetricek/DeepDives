module Behavior
open TickSpec

// ----------------------------------------------------------------------------
// Listing 12.8 – Specification of Scrabble scoring
// ----------------------------------------------------------------------------

let mutable actual = 0

let [<Given>] ``an empty scrabble board`` () = 
    actual <- 0

let [<When>] ``player (\d+) plays "([A-Z]+)" at (\d+[A-Z])`` 
        (player:int, word:string, location:string) = 
    actual <- wordPoints word

let [<Then>] ``she scores (\d+)`` (expected:int) =     
    Assert.AreEqual(expected,actual)
