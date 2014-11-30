module TestDoubles
open FsUnit
open NUnit.Framework

// ----------------------------------------------------------------------------
// 12.3.2 Avoiding dependencies with test doubles
// ----------------------------------------------------------------------------
module Original = 

  let CalculateTax(rawPrice) =              
      rawPrice * 0.25M

  let GetPriceWithTax (rawPrice) =
      rawPrice + CalculateTax(rawPrice)

module Decoupled = 
  
  // Taking dependencies as arguments

  let GetPriceWithTax calculateTax rawPrice =
      rawPrice + calculateTax(rawPrice)

  // Unit test for 'GetPriceWithTax'

  let [<Test>]  ``price with tax should include tax`` () =
      let price, tax = 100.0M, 15.0M
      let calculateTax _ = tax
      Assert.AreEqual(price+tax, GetPriceWithTax calculateTax price) 

// ----------------------------------------------------------------------------
// Listing 12.6 – Mocking IList interface with Foq
// ----------------------------------------------------------------------------

module FoqSamples = 
  open Foq
  open System.Collections.Generic

  let [<Test>] ``mock with multiple members`` () =
      let xs =
          Mock<IList<char>>.With(fun xs ->
              <@ xs.Count --> 2 
                 xs.[0] --> '0'
                 xs.[1] --> '1'
                 xs.Contains(any()) --> true
              @>
          )
      Assert.AreEqual(2, xs.Count)
      Assert.AreEqual('0', xs.[0])
      Assert.AreEqual('1', xs.[1])
      Assert.AreEqual(true, xs.Contains('0'))
