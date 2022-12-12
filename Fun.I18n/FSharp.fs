﻿module internal Fun.I18n.FSharp

open System.IO
open System.Text.Json
open Fun.I18n.Utils


[<RequireQualifiedAccess>]
type KV =
    | Double of name: string
    | String of name: string
    | Format of name: string
    | SubObj of name: string * KV list


let generateSourceCode (sourceFile: string) (targetDir: string) (targetNamespace: string) =

    let jsonFile = File.ReadAllText sourceFile
    let rootName = Path.GetFileName(sourceFile).Split(".")[0]
    let rootElement = JsonDocument.Parse(jsonFile).RootElement


    let rec parseJsonKV (name: string) (element: JsonElement) =
        match element.ValueKind with
        | JsonValueKind.Number -> KV.Double name      

        | JsonValueKind.String ->
            if formatHoleRegex.IsMatch(element.GetString()) then KV.Format name
            else KV.String name

        | JsonValueKind.Object ->
            KV.SubObj(
                name,
                element.EnumerateObject()
                |> Seq.map (fun obj -> parseJsonKV obj.Name obj.Value)
                |> Seq.toList
            )

        | _ -> failwithf "Unsuppported json value %A" element.ValueKind

    
    let makePrivateVar (kv: KV) =
        match kv with
        | KV.Double name
        | KV.String name
        | KV.Format name
        | KV.SubObj (name, _) ->  $"let _{name} = ref None" 

    let makePublicMethod (kv: KV) =
        match kv with
        | KV.Double name
        | KV.String name ->  $"member _.{name} = _{name} =? (fun _ -> element |> JsonElement.getSafeStringProperty \"{name}\" fallbackElement)" 
        | KV.Format name ->  $"member _.{name}([<ParamArray>] ps: obj []) = String.Format(_{name} =? (fun _ -> element |> JsonElement.getSafeStringProperty \"{name}\" fallbackElement), ps)" 
        | KV.SubObj (name, _) -> $"member _.{name} = _{name} =? (fun _ -> {name}(JsonElement.getProperty \"{name}\" element, JsonElement.getProperty \"{name}\" fallbackElement))"


    let lines = System.Collections.Generic.List()

    let rec makeSourceCode level (name: string) (kvs: KV list) =
        let tyName = if level = 0 then "type" else "and"

        $"{tyName} {name} (element: JsonElement option, fallbackElement: JsonElement option) = " |> lines.Add
        kvs |> Seq.map makePrivateVar |> Seq.map (fun x -> "    " + x) |> lines.AddRange
        "" |> lines.Add

        if level = 0 then
            let code = $"""
    new(location: string, defaultCulture: CultureInfo, ?targetCulture) =
        let name = "{name}"

        let tryGetCultureJson (cultureInfo: CultureInfo) =
            let jsonFilePath = Path.Combine(location, name + "." + cultureInfo.Name + ".json")
            if File.Exists(jsonFilePath) then
                JsonDocument.Parse(File.ReadAllText(jsonFilePath)).RootElement |> Some
            else
                let jsonFilePath = Path.Combine(location, name + "." + cultureInfo.TwoLetterISOLanguageName + ".json")
                if File.Exists(jsonFilePath) then
                    JsonDocument.Parse(File.ReadAllText(jsonFilePath)).RootElement |> Some
                else
                    None

        let element = tryGetCultureJson (targetCulture |> Option.defaultValue CultureInfo.CurrentUICulture)
        let fallbackElement = tryGetCultureJson defaultCulture

        I18nDemo(element, fallbackElement)
                """

            code.Split "\r\n"
            |> lines.AddRange

        kvs |> Seq.map makePublicMethod |> Seq.map (fun x -> "    " + x) |> lines.AddRange
        "" |> lines.Add

        kvs
        |> Seq.iter (function
            | KV.SubObj (name, kvs) -> makeSourceCode (level + 1) name kvs
            | _ -> ()
        )


    let kv = parseJsonKV rootName rootElement
    
    match kv with
    | KV.SubObj (name, kvs) -> makeSourceCode 0 name kvs
    | _ -> failwith "Expected an json object"

    File.WriteAllLines(
        targetDir </> rootName + ".fs",
        [
            $"""// <auto-generated/>
namespace {targetNamespace}

open System
open System.IO
open System.Text.Json
open System.Globalization

[<AutoOpen>]
module private Utils =

    module Option =
        let defaultBind fn op =
            match op with
            | None -> fn()
            | _ -> op

    module JsonElement =
        let getProperty (name: string) (ele: JsonElement option) =
            ele |> Option.bind (fun ele ->
                match ele.TryGetProperty name with
                | true, x -> Some x
                | _ -> None
            )

        let getSafeProperty name fallbackElement element =
            element
            |> getProperty name
            |> Option.defaultBind (fun _ -> fallbackElement |> getProperty name)

        let getSafeStringProperty name fallbackElement element =
            getSafeProperty name fallbackElement element
            |> Option.map (fun x -> x.GetString())
            |> Option.defaultValue ""

    let (=?) (data: 'Data option ref) fn =
        match data.Value with
        | Some x -> x
        | None -> 
            data.Value <- Some (fn())
            data.Value.Value
            """
            yield! lines
        ]
    )
