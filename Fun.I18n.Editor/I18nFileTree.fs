[<AutoOpen>]
module Fun.I18n.Editor.I18nFileTree

open System
open Feliz
open Feliz.Recoil
open Fable.Core.JsInterop
open Fable.SimpleJson
open Fun.I18n.Provider.Fable

open type Html
open type prop


[<ReactComponent>]
let I18nFileTree () =
    let i18n = Recoil.useValue Stores.i18n
    let files, setFiles = Recoil.useState Stores.files
    let selectedFile, setSelectedFile = Recoil.useState Stores.selectedFile
    let defaultLocale, setDefaltLocale = React.useState "en"
    let selectedPath, setSelectedPath = Recoil.useState Stores.selectedPath
    let parsedFiles, setParseFiles = Recoil.useState Stores.parsedFiles
    let i18nJson, setI18nJson = React.useState None

    let setI18nFiles (files: Browser.Types.File list) =
        let files = files |> List.filter (fun file -> file.name.EndsWith(".i18n.json", StringComparison.OrdinalIgnoreCase))
        files |> setFiles
        
        let mutable maps = parsedFiles
        files
        |> List.iteri (fun i file ->
            let fileReader = Browser.Dom.FileReader.Create()
            fileReader.onload <- fun e ->
                e.target?result |> Utils.parseToI18nMap |> fun x -> Map.add file.name x maps |> fun x -> maps <- x
                if i = files.Length - 1 then
                    setParseFiles maps
            fileReader.readAsText(file))

        match files with
        | file::_ ->
            let fileReader = Browser.Dom.FileReader.Create()
            fileReader.onload <- fun e ->
                e.target?result |> SimpleJson.parse |> Some |> setI18nJson
            fileReader.readAsText(file) 
        | _ ->
            ()

    let rec renderI18nJson (path: string) (name: string) (json: Json) =
        let (</>) str1 str2 =
            if String.IsNullOrEmpty str1 then str2
            elif String.IsNullOrEmpty str2 then str1
            else str1 + ":" + str2
        match json with
        | Json.JObject map ->
            div [
                children [
                    div [
                        text name
                        onClick (fun e -> e.stopPropagation(); path </> name |> Some |> setSelectedPath)
                    ]
                    div [
                        classes [ Tw.``ml-2`` ]
                        children [
                            for name', json in Map.toList map do
                                yield renderI18nJson (path </> name) name' json
                        ]
                    ]
                ]
            ]
        | _ ->
            div [
                text name
                onClick (fun e -> e.stopPropagation(); path </> name |> Some |> setSelectedPath)
            ]

    div [
        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch`` ]
        children [
            div [
                children [
                    input [
                        type' "file"
                        multiple true
                        custom ("webkitdirectory", "")
                        //classes [ Tw.hidden ]
                        onChange setI18nFiles
                        accept ".json"
                    ]
                ]
            ]
            div [
                text i18n.App.Commands.SelectFilesMsg
                classes [ Tw.``text-xs``; Tw.``text-gray-300``; Tw.``bg-yellow-100``; Tw.``text-center`` ]
            ]
            div [
                classes [ Tw.``flex-1``; Tw.``overflow-auto`` ]
                children [
                    div (string selectedPath)
                    match i18nJson with
                    | None -> ()
                    | Some json -> renderI18nJson "" "" json
                ]
            ]
        ]
    ]
