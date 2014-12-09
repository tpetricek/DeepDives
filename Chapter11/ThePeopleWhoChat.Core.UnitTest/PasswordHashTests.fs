namespace ThePeopleWhoChat.Core.UnitTest

    open NUnit.Framework
    open ThePeopleWhoChat.Core

    [<TestFixture>]
    type PasswordHashTests() =

        [<Test>]
        member x.``Hashed password isn't the password``() =
            let hash = PasswordHash.GenerateHashedPassword("hoopleD00pl3")
            Assert.AreNotEqual("hoopleD00pl3", hash)

        [<Test>]
        member x.``Password verifies against previously hashed password``() =
            let hash = PasswordHash.GenerateHashedPassword("hoopleD00pl3")
            Assert.IsTrue(PasswordHash.VerifyPassword("hoopleD00pl3",hash))

        [<Test>]
        member x.``Incorrect password does not verify against previously hashed password``() =
            let hash = PasswordHash.GenerateHashedPassword("hoopleD00pl3")
            Assert.IsFalse(PasswordHash.VerifyPassword("hoopleD00pl4",hash))