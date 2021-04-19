namespace Fun.I18n.Provider

// File watcher implementation taken from FSharp.Data
open System.IO
open System.Collections.Generic


type private FileWatcher(path) =

    let subscriptions = Dictionary<string, unit -> unit>()

    let getLastWrite() = File.GetLastWriteTime path
    let mutable lastWrite = getLastWrite()

    let watcher =
        new FileSystemWatcher(
            Filter = Path.GetFileName path,
            Path = Path.GetDirectoryName path,
            EnableRaisingEvents = true)

    let checkForChanges action _ =
        let curr = getLastWrite()

        if lastWrite <> curr then
            // log (sprintf "File %s: %s" action path)
            lastWrite <- curr
            // creating a copy since the handler can be unsubscribed during the iteration
            let handlers = subscriptions.Values |> Seq.toArray
            for handler in handlers do
                handler()

    do
        watcher.Changed.Add (checkForChanges "changed")
        watcher.Renamed.Add (checkForChanges "renamed")
        watcher.Deleted.Add (checkForChanges "deleted")

    member __.Subscribe(name, action) =
        subscriptions.Add(name, action)

    member __.Unsubscribe(name) =
        if subscriptions.Remove(name) then
            // log (sprintf "Unsubscribed %s from %s watcher" name path)
            if subscriptions.Count = 0 then
                // log (sprintf "Disposing %s watcher" path)
                watcher.Dispose()
                true
            else
                false
        else
            false
