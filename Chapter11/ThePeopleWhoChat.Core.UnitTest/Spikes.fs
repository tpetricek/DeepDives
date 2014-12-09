namespace ThePeopleWhoChat.Core.UnitTest
    
    open System
    open System.Threading
    open System.Configuration
    open ThePeopleWhoChat.Core
    open ThePeopleWhoChat.Data
    open NUnit.Framework

    [<TestFixture>]
    type Spikes() =

        [<Test>]
        [<Ignore>]
        member this.``spikes to be enabled & run as and when required``() = ()

        [<Test>]
        [<Ignore>]
        member this.``ravenDb will respect millisecond time differences in orderBy``() =
            // note this spike requires the database to be empty. run deleteall in the AdminTool before running this
            let url = ConfigurationManager.AppSettings.[Consts.DbUrlSettingKey]
            let data = ChatDataConnection(url) :> IChatServiceClient
            let token = data.Login("root","password1")
            let roomId = data.AddRoom(token,{Id = null; name = "test"; description = "test"})
            data.EnterRoom(token, roomId)

            for x in 0..100 do
                data.PostMessage(token,(sprintf "%d" x))
                Thread.Sleep(5)

            let msgs = data.GetMessages(token, DateTime.MinValue)
            for x in 0..100 do
                Assert.AreEqual((sprintf "%d" x), msgs.[x].rawMessage)

            let msgs2 = data.GetMessages(token, msgs.[50].timestamp)
            Assert.AreEqual(50, msgs2.Length)
            for x in 51..100 do
                Assert.AreEqual((sprintf "%d" x), msgs2.[x-51].rawMessage)