namespace ThePeopleWhoChat.Core.UnitTest

    open NUnit.Framework
    open ThePeopleWhoChat.Core

    [<TestFixture>]
    type LineParserTests() = 

        [<Test>]
        member x.``empty line``() =
            let line = ""
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| |], result)

        [<Test>]
        member x.``plain line parses correctly``() =
            let line = "hello F#ers, how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers,"; "how"; "are"; "you?" |], result)

        [<Test>]
        member x.``line with quoted section parses correctly``() =
            let line = "hello F#ers, \"how are you?\""
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers,"; "how are you?" |], result)

        [<Test>]
        member x.``line with an escaped quote parses correctly``() =
            let line = "hello \"\"F#ers\"\", how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "\"F#ers\","; "how"; "are"; "you?" |], result)

        [<Test>]
        member x.``line with unclosed quoted section keeps last block``() =
            let line = "hello F#ers, \"how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers,"; "how are you?" |], result)

        [<Test>]
        member x.``line with quotes within a word treats them like escaped``() =
            let line = "hello F\"ers, how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F\"ers,"; "how"; "are"; "you?" |], result)

        [<Test>]
        member x.``quotes in word in quoted section treated as escape``() =
            let line = "hello \"F#ers, how a\"re\" you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers, how a\"re"; "you?" |], result)

        [<Test>]
        member x.``line starting with a quote``() =
            let line = "\"hello F#ers,\" how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello F#ers,"; "how"; "are"; "you?" |], result)

        [<Test>]
        member x.``line starting with an escape``() =
            let line = "\"\"hello F#ers, how are you?"
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "\"hello"; "F#ers,"; "how"; "are"; "you?" |], result)

        [<Test>]
        member x.``line ending with a quote``() =
            let line = "hello F#ers, \"how are you?\""
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers,"; "how are you?" |], result)

        [<Test>]
        member x.``line ending with an escape``() =
            let line = "hello F#ers, how are you?\"\""
            let result = LineParser.SpaceSeperatedParser.Parse(line) |> Array.ofSeq
            Assert.AreEqual([| "hello"; "F#ers,"; "how"; "are"; "you?\"" |], result)

