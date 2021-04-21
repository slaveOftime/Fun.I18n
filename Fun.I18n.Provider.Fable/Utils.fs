﻿module Fun.I18n.Provider.Fable.Utils

open Fable.Core


[<Emit("JSON.parse($0)")>]
let jsonParse (json: string) = obj()

[<Emit("Object.entries($0)")>]
let objectEntries (data: obj): (string * obj) [] = jsNative

[<Emit("typeof $0 === 'object'")>]
let isObject (data: obj): bool = jsNative

[<Emit("typeof $0 === 'string'")>]
let isString (data: obj): bool = jsNative

[<Emit("window.$funI18n = $0")>]
let setFunI18nToWindow (data: obj) = jsNative


let parseToMap (jsonString: string) : Map<string, string> =
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


let translate (bundle: Map<string, string>) (path: string) (key: string) =
    let path = if path.Length > 0 then path + ":" + key else key
    bundle
    |> Map.tryFind path
    |> Option.defaultValue key


let translateWith (forSmartCount: bool) (bundle: Map<string, string>) (path: string) (fieldDefs: (string * string) list) (args: obj list) =
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
    |> List.mapi (fun i arg ->
        match fieldDefs.[i] with
        | name, _ when name = "smart_count" -> "%{smart_count}", string arg
        | name, "String"                    -> "%s{" + name + "}", string arg
        | name, "Int"                       -> "%d{" + name + "}", string arg
        | name, "Float"                     -> "%f{" + name + "}", string arg
        | name, _                           -> "%{" + name + "}", string arg)
    |> List.fold
        (fun (state: string) (name, arg) -> state.Replace(name, arg))
        (unformattedString)


let setUp () = 
    setFunI18nToWindow
        {|
            parseToMap = parseToMap
            translate = translate
            translateWith = translateWith
        |}
