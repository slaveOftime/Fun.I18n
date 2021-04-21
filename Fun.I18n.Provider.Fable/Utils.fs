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

let parseToMapFable (jsonString: string) : Map<string, string> =
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


#if FABLE_COMPILER
open Fable.Core

[<Emit("window.funI18nParseToMap = $0")>]
let addParseToJsonToBrowserWindow (fn: string -> Map<string, string>) = jsNative

addParseToJsonToBrowserWindow parseToMapFable
#endif
