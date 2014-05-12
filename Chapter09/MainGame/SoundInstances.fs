namespace PracticalFSharp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Audio

/// Keep track of SoundEffectInstances, allowing to pause and resume them.
/// Instances which have completed are disposed.
type SoundEffectInstances() =
    let instances = ResizeArray<SoundEffectInstance>()

    member this.Play(sound : SoundEffect) = 
        let instance =
            sound.CreateInstance()
        instance.Play()
        instances.Add(instance)

    member this.PauseAll() =
        for instance in instances do
            instance.Pause()

    member this.ResumeAll() =
        for instance in instances do
            instance.Resume()

    member this.Update() =
        for instance in instances do
            if instance.State = SoundState.Stopped then
                instance.Dispose()

        instances.RemoveAll(fun instance -> instance.IsDisposed)
        |> ignore

    interface System.IDisposable with
        member this.Dispose() =
            for instance in instances do
                instance.Dispose()
            instances.Clear()

    member this.Dispose() =
        (this :> System.IDisposable).Dispose()