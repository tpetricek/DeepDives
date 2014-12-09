namespace ThePeopleWhoChat.Core

    open System
    open System.Text
    open System.Security.Cryptography
    open System.Runtime.InteropServices

    type PasswordHash() =
        static let hashProvider = new SHA256Managed()
        static let saltLength = 4
        static let minPasswordLength = 7
        static let separatorChar = ' '

        static member private ComputeHash(data:byte[],salt:byte[]) =
            let combined = Array.append data salt
            hashProvider.ComputeHash(combined)

        static member private GenerateHash(data:byte[]) =
            let salt = Array.create saltLength 0uy
            use random = new RNGCryptoServiceProvider()
            random.GetNonZeroBytes(salt)
            PasswordHash.ComputeHash(data, salt),salt

        static member private VerifyHash(data:byte[],hash:byte[],salt:byte[]) =
            let newHash = PasswordHash.ComputeHash(data, salt)
            newHash = hash

        static member GenerateHashedPassword(password:string) =
            if password.Length < minPasswordLength then failwith (String.Format("Passwords must be {0} characters or longer",minPasswordLength))
            let hash,salt = PasswordHash.GenerateHash(Encoding.UTF8.GetBytes(password))
            String.Format("{0}{2}{1}", Convert.ToBase64String(hash), Convert.ToBase64String(salt), separatorChar)

        static member VerifyPassword(password:string,saltedHash:string) =
            match saltedHash.Split(separatorChar) with
            | [| hash; salt |] -> PasswordHash.VerifyHash(Encoding.UTF8.GetBytes(password),
                                    Convert.FromBase64String(hash), Convert.FromBase64String(salt))
            | _ -> false
