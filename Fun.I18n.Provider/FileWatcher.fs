﻿namespace Fun.I18n.Provider

// File watcher implementation taken from FSharp.Data
open System
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


module FileWatcher =
    let private watchers = Dictionary<string, FileWatcher>()
    
    // sets up a filesystem watcher that calls the invalidate function whenever the file changes
    let watchForChanges path (owner, onChange) =
        let watcher =
            lock watchers (fun () ->
                match watchers.TryGetValue(path) with
                | true, watcher ->
                    // log (sprintf "Reusing %s watcher" path)
                    watcher.Subscribe(owner, onChange)
                    watcher
    
                | false, _ ->
                    // log (sprintf "Setting up %s watcher" path)
                    let watcher = FileWatcher path
                    watcher.Subscribe(owner, onChange)
                    watchers.Add(path, watcher)
                    watcher)
    
        { new IDisposable with
            member __.Dispose() =
                lock watchers (fun () ->
                    if watcher.Unsubscribe(owner) then
                        watchers.Remove(path) |> ignore) }
