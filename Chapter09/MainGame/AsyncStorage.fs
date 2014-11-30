module AsyncStorage

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Storage
open System
open System.Xml.Serialization

// async computation to show the device selector, if more than one storage device is available.
let showSelector = Async.FromBeginEnd(StorageDevice.BeginShowSelector, StorageDevice.EndShowSelector)

// Waits until the guide is no longer visible, then show the storage device selector.
let forceShowSelector =
    async {
        let! dev = showSelector // Asynchronously call the showSelector async computation.
        return
            match dev with
            | null -> None // Idiomatic F# code: use Option instead for types that use null as a value.
            | _ -> Some dev
    }
    |> Dialogs.doWhenGuideNoLongerVisible

// async computation to open the file container.
let openContainer name (dev : StorageDevice) = Async.FromBeginEnd((fun (callback, arg) -> dev.BeginOpenContainer(name, callback, arg)), dev.EndOpenContainer)

// Tries to load data from an XML file, then deserialize it to an object.
// Does not raise exceptions, failures are indicated by None values.
// The rationale is that failures to load are not exceptional.
let tryLoadData<'T> (file : IO.Stream) =
    let serializer = new XmlSerializer(typeof<'T>)
    try
        serializer.Deserialize(file) :?> 'T
        |> Some
    with
    | _ -> None

// Saves the data to an XML file. Failures raise exceptions.
// Failures while saving are exceptional.
let saveData (file : IO.Stream) (data : 'T) =
    let serializer = new XmlSerializer(typeof<'T>)
    serializer.Serialize(file, data)

// Serialization-friendly type to represent an entry in the score board.
type NamedScore =
    class
        val mutable name : string
        val mutable score : int

        new() = { name = "Unknown"; score = 0 }
        
        member this.Name
            with get() = this.name
            and set(x) = this.name <- x
        
        member this.Score
            with get() = this.score
            and set(x) = this.score <- x
    end

// Serialization-friendly type to represent score boards.
type NamedScores =
    class
        val mutable scores : ResizeArray<NamedScore>

        new() = { scores = new ResizeArray<NamedScore>() }

        member this.NumScores = this.scores.Count

        member this.GetRank(score) =
            this.scores
            |> Seq.tryFindIndex (fun s -> s.Score < score)

        member this.InsertScore(score : NamedScore) =
            let pos = this.GetRank(score.Score)
            match pos with
            | None -> this.scores.Add(score)
            | Some idx -> this.scores.Insert(idx, score)

        member this.Truncate(num) =
            if this.scores.Count > num then
                this.scores.RemoveRange(num, this.scores.Count - num)

        member this.GetAsPairs() =
            this.scores
            |> Seq.map (fun namedScore ->
                (namedScore.Name, namedScore.Score))
            |> Array.ofSeq
    end

// The result of an I/O operation.
type IOResult<'T> =
    | Successful of 'T // Operation succeeded, holds the data that was loaded.
    | BadData // Could not load data, it was ill-formed.
    | DeviceDisconnected // Could not load data, possibly because the user yanked the memory card or USB stick.

// Tries to asychronously open a container and then load a file.
let loadScores dev containerName fileName =
    async {
        try
            use! container = openContainer containerName dev
            if container.FileExists(fileName) then
                use file = container.OpenFile(fileName, IO.FileMode.Open)
                return
                    match tryLoadData<NamedScores> file with
                    | None -> BadData
                    | Some x -> Successful x
            else
                return Successful(new NamedScores())
        with
        | :? InvalidOperationException
        | :? StorageDeviceNotConnectedException -> return DeviceDisconnected
    }

// Tries to asynchronously save a score table to a file.
let saveScores dev containerName fileName (scores : NamedScores) =
    async {
        try
            use! container = openContainer containerName dev
            use file = container.OpenFile(fileName, IO.FileMode.Create)
            saveData file scores
            return true
        with
        | :? InvalidOperationException
        | :? StorageDeviceNotConnectedException -> return false
    }

