module Fun.I18n.Provider.Utils


type ParseJsonToMap = string -> Map<string, string>


#if !FABLE_COMPILER
let parseToMap: ParseJsonToMap =
    fun jsonString ->
        let rec foldJsonObjectToMap path (state: Map<string, string>) (keyValues: (string * JsonParser.Json) list) =
            keyValues
            |> List.fold
                (fun state (k, v) ->
                    let prefix =
                        match path with
                        | "" -> k
                        | _ -> path + ":" + k
                    match v with
                    | JsonParser.Object kvs -> foldJsonObjectToMap prefix state kvs
                    | JsonParser.String v -> state |> Map.add prefix v
                    | _ -> state)
                state
        JsonParser.tryParse jsonString
        |> function
            | Some (JsonParser.Object kvs) -> foldJsonObjectToMap "" Map.empty kvs
            | _ -> Map.empty
            
#else
open Fable.Core

[<Emit("JSON.parse($0)")>]
let jsonParse (json: string) = obj()

[<Emit("Object.entries($0)")>]
let objectEntries (data: obj): (string * obj) [] = jsNative

[<Emit("typeof $0 === 'object'")>]
let isObject (data: obj): bool = jsNative

let parseToMap: ParseJsonToMap =
    fun jsonString ->
        let json = jsonParse jsonString
        let rec foldJsonObjectToMap path (state: Map<string, string>) (keyValues: (string * obj) []) =
            keyValues
            |> Array.fold
                (fun state (k, v) ->
                    let prefix =
                        match path with
                        | "" -> k
                        | _ -> path + ":" + k
                    if isObject v then foldJsonObjectToMap prefix state (objectEntries v)
                    else state |> Map.add prefix (string v))
                state
        jsonParse jsonString
        |> objectEntries
        |> foldJsonObjectToMap "" Map.empty
#endif
