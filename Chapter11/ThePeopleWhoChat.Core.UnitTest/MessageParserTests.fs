namespace ThePeopleWhoChat.Core.UnitTest
    
    open System
    open ThePeopleWhoChat.Core
    open NUnit.Framework

    [<TestFixture>]
    type MessageParserTests() =

        static let makeHtml url desc =
            sprintf "<a href=\"%s\">%s</a>" url desc

        [<Test>]
        member x.``Empty message parses as empty message``() =
            let result = MessageParser.Parse("")
            Assert.AreEqual("", result)

        [<Test>]
        member x.``Plain message parses as plain text``() =
            let msg = "this is a simple message"
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(msg, result)

        [<Test>]
        member x.``Just URL parses as URL markup``() =
            let msg = "http://wibble.net/something?text=hello"
            let exp = makeHtml msg msg
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``Just URL with port parses as URL markup``() =
            let msg = "http://wibble.net:80/something?text=hello"
            let exp = makeHtml msg msg
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``Just URL with IP and port parses as URL markup``() =
            let msg = "http://127.0.0.1:80/something?text=hello"
            let exp = makeHtml msg msg
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``URL with following text``() =
            let url = "https://madness.com/something.htm"
            let follow = " <- check this out!"
            let msg = url + follow
            let exp = (makeHtml url url) + follow
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``URL with preceeding text``() =
            let url = "https://madness.com/something.htm"
            let preceed = "nice site: "
            let msg = preceed + url
            let exp = preceed + (makeHtml url url)
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``URL with enveloping text``() =
            let url = "http://www.madness.com/something.htm"
            let preceed = "nice site: "
            let follow = " let me know what you think"
            let msg = preceed + url + follow
            let exp = preceed + (makeHtml url url) + follow
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``Multiple URLs``() =
            let url1 = "http://www.madness.com/something.htm"
            let url2 = "https://news.bbc.co.uk/"
            let msg = url1 + " " + url2
            let exp = (makeHtml url1 url1) + " " + (makeHtml url2 url2)
            let result = MessageParser.Parse(msg)
            Assert.AreEqual(exp, result)

        [<Test>]
        member x.``Url without scheme is expanded to http but displayed as os``() =
            let url = "www.large.com/index.hml"
            let exp = makeHtml ("http://" + url) url
            let result = MessageParser.Parse(url)
            Assert.AreEqual(exp, result)