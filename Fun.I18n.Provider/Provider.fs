﻿namespace Fun.I18n.Provider

open System.Reflection
open System.Text.RegularExpressions
open Microsoft.FSharp.Core.CompilerServices
open Fable.SimpleHttp

open ProviderImplementation.ProvidedTypes


[<TypeProvider>]
type TypeProvider (config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config)

    let asm = Assembly.GetExecutingAssembly()
    let ns = "Fun.I18n.Provider"

    let generator = ProvidedTypeDefinition(asm, ns, "I18nProvider", Some typeof<obj>, isErased = true)
    let staticParams = [
        ProvidedStaticParameter("i18nFilePathOrUrl", typeof<string>)
        ProvidedStaticParameter("forFable", typeof<bool>)
    ]

    let watcherSubscriptions = System.Collections.Concurrent.ConcurrentDictionary<string, System.IDisposable>()


    let buildTypes typeName (pVals:obj[]) =
        match pVals with
        | [| :? string as arg; :? bool as forFable |] ->
            if Regex.IsMatch(arg, "^https?://") then
                async {
                    let! (status, res) = Http.get arg
                    if status <> 200 then
                        return failwithf "URL %s returned %i status code" arg status
                    return
                        match Generator.createProviderTypeDefinition forFable asm ns typeName res with
                        | Some t -> t
                        | None -> failwithf "Response from URL %s is not a valid JSON: %s" arg res
                } |> Async.RunSynchronously

            else
                let content =
                    if arg.StartsWith("{") || arg.StartsWith("[") then arg
                    else
                        let filepath =
                            if System.IO.Path.IsPathRooted arg then
                                arg
                            else
                                System.IO.Path.GetFullPath(System.IO.Path.Combine(config.ResolutionFolder, arg))

                        let weakRef = System.WeakReference<_>(this)

                        let _  =
                            watcherSubscriptions.GetOrAdd
                                (typeName
                                ,fun _ ->
                                    FileWatcher.watchForChanges
                                        (filepath)
                                        (typeName + this.GetHashCode().ToString()
                                        ,fun () ->
                                            match weakRef.TryGetTarget() with
                                            | true, t -> t.Invalidate()
                                            | _ -> ()))

                        System.IO.File.ReadAllText(filepath, System.Text.Encoding.UTF8)

                match Generator.createProviderTypeDefinition forFable asm ns typeName content with
                | Some t -> t
                | None -> failwithf "Local sample is not a valid JSON"

        | _ -> failwith "unexpected parameter values"


    do this.Disposing.Add(fun _ -> watcherSubscriptions |> Seq.iter ( fun kv -> kv.Value.Dispose()) )
    do generator.DefineStaticParameters(staticParams, buildTypes)
    do this.AddNamespace(ns, [generator])


[<assembly:TypeProviderAssembly>]
do ()
