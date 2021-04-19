module Fun.I18n.Provider.Utils

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes

open ProviderDsl
open Fable.Core


type internal I18nBundle =
    { Locale: string
      JsonData: JsonParser.Json }


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


let firstToUpper (s: string) = s.[0..0].ToUpper() + s.[1..]

let callAction prm (x:System.Action<'a>) = x.Invoke(prm)

// always return root object
let getterCode name = fun (args: Expr list) -> <@@ %%args.Head @@>


#if FABLE_COMPILER
[<Emit("((x,a)=>{let o=(x||{});o[a[0]]=a[1];return o})($1,$0)")>]
let addkv ((propertyName, value): string * obj) (data: obj = obj()
#else
let addkv ((propertyName, value): string * obj) (data: obj) =
    let prop = data.GetType().GetProperty(propertyName)
    prop.SetValue(data, value)
    data
#endif


#if FABLE_COMPILER
[<Emit("$1[$0]")>]
let getter (propertyName: string) (data: obj) = obj()
#else
let getter (propertyName: string) (data: obj) = data.GetType().GetProperty(propertyName).GetValue(data)
#endif

#if FABLE_COMPILER
[<Emit("$2[$0] = $1")>]
let setter (propertyName: string) (value: obj) (data: obj) = ()
#else
let setter (propertyName: string) (value: obj) (data: obj) =
    let prop = data.GetType().GetProperty(propertyName)
    prop.SetValue(data, value)
#endif


let rec makeMember locale isRoot (ns:string) (name, json) =
    let path = if ns.Length > 0 then ns + "." + name else name
    match json with
    | JsonParser.Null -> []
    | JsonParser.Bool _ -> []
    | JsonParser.Number _ -> []
    | JsonParser.String value ->
        let memberName = name

        let parameterNames =
            Regex.Matches(value, "%{(.*?)}", RegexOptions.IgnoreCase)
            |> Seq.cast<System.Text.RegularExpressions.Match>
            |> Seq.map (fun m -> m.Groups.[1].Value )
            |> Seq.distinct
            |> Seq.toList

        let hasMultipleTranslations = value.Contains("||||")
        let hasSmartCount = parameterNames |> Seq.contains "smart_count"

        [ 
            Property
                (memberName, String, false
                ,fun args -> 
                    <@@
                        let bundle: I18nBundle = %%args.[0]
                        match bundle.JsonData with
                        | JsonParser.Object kvs ->
                            kvs
                            |> List.tryPick (fun (key, v) -> 
                                match key = name, v with
                                | true, JsonParser.String v -> Some v
                                | _ -> None)
                            |> Option.defaultValue name
                        | _ ->
                            name
                    @@>)
        ]

    | JsonParser.Array _ -> []
    | JsonParser.Object members ->
        let members = 
            [
                yield! members |> List.collect (makeMember locale false path)
                Constructor
                    ([]
                    ,fun _ ->
                        <@@
                            { JsonData = JsonParser.Object members
                              Locale = locale }
                        @@>)
            ]
        let nestedTypeName = firstToUpper name
        let nestedType = makeCustomType(nestedTypeName, members)
        [ ChildType nestedType
          Property(name, Custom nestedType, false, getterCode name) ]


let createProviderGeneratorTypeDefinition asm ns typeName sample =
    let makeRootType basicMembers =
        makeRootType(asm, ns, typeName, [
            yield! basicMembers |> List.collect (makeMember "" true "")
            Property
                ("Locale", String, false
                ,fun args -> <@@ (%%args.[0]: I18nBundle).Locale @@>)
            Constructor
                ([ "jsonString", String; "locale", String ]
                ,fun args -> 
                    <@@
                        let json =
                            match JsonParser.parse %%args.[0] with
                            | None ->  JsonParser.Json.Null
                            | Some x -> x
                        { JsonData = json
                          Locale = %%args.[1] }
                    @@>)
        ])

    match JsonParser.parse sample with
    | Some(JsonParser.Object members) -> makeRootType members |> Some
    | _ -> None
