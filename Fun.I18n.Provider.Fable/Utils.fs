module Fun.I18n.Provider.Fable.Utils

open Fable.Core


[<Emit("JSON.parse($0)")>]
let jsonParse (json: string) = obj()

[<Emit("Object.entries($0)")>]
let objectEntries (data: obj): (string * obj) [] = jsNative

[<Emit("typeof $0 === 'object'")>]
let isObject (data: obj): bool = jsNative

[<Emit("typeof $0 === 'string'")>]
let isString (data: obj): bool = jsNative

[<Emit("$0[$1] = $2")>]
let addDataToObject (target: 'T) key (data: obj): unit = jsNative


let createI18nMap (jsonString: string) =
    let rec foldJsonObjectToMap path (state: Map<string, string>) (keyValues: (string * obj) []) =
        keyValues
        |> Array.fold
            (fun state (key, value) ->
                let prefix =
                    match path with
                    | "" -> key
                    | _ -> path + ":" + key
                if isObject value then foldJsonObjectToMap prefix state (objectEntries value)
                elif isString value then state |> Map.add prefix (string value)
                else state)
            state
    jsonParse jsonString
    |> objectEntries
    |> foldJsonObjectToMap "" Map.empty


let tryTranslate (bundle: Map<string, string>) (path: string) (key: string) =
    let path = if path.Length > 0 then path + ":" + key else key
    bundle
    |> Map.tryFind path

let translate (bundle: Map<string, string>) (path: string) (key: string) =
    tryTranslate bundle path key
    |> Option.defaultWith (fun () -> if path.Length > 0 then path + ":" + key else key)


let translateWith (forSmartCount: bool) (bundle: Map<string, string>) (path: string) (fieldDefs: string list) (args: obj list) =
    let SMART_COUNT_SPLITER = "||||"

    let unformattedString =
        let value = translate bundle "" path
        if forSmartCount && value.Contains SMART_COUNT_SPLITER then
            let index = value.IndexOf SMART_COUNT_SPLITER
            let count = args.[0] :?> int
            if count = 0 || count = 1 then
                value.Substring(0, index).Trim()
            else
                value.Substring(index + SMART_COUNT_SPLITER.Length).Trim()
        else
            value

    args
    |> List.mapi (fun i arg -> "%{" + fieldDefs.[i] + "}", string arg)
    |> List.fold
        (fun (state: string) (name, arg) -> state.Replace(name, arg))
        (unformattedString)


let overrideTranslation (bundle: Map<string, string>) (newBundle: Map<string, string>) =
    bundle
    |> Map.toList 
    |> List.map fst
    |> List.fold
        (fun state key ->
            newBundle
            |> Map.tryFind key
            |> function
                | None -> state
                | Some v -> state |> Map.add key v)
        bundle


let parseToI18nMap (jsonString: string) : Map<string, string> =
    let bundle = createI18nMap jsonString

    addDataToObject bundle "$i18n"
        {|
            translate = translate
            tryTranslate = tryTranslate
            translateWith = translateWith
        |}
        
    bundle


// Use this to pull dependencies for fable
let inline createI18n (i18n: string -> 'I18N) (jsonString: string): 'I18N =
    let _ = parseToI18nMap
    i18n jsonString


let inline createI18nWith (defaultI18n: 'I18nProvidedType) (jsonString: string): 'I18nProvidedType =
    let bundle = defaultI18n |> unbox<Map<string, string>>
    let newBundle = parseToI18nMap jsonString
    let overridedBundle = overrideTranslation bundle newBundle

    addDataToObject overridedBundle "$i18n"
        {|
            translate = translate
            translateWith = translateWith
        |}

    overridedBundle |> unbox<'I18nProvidedType>
