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
    let defaultFileName, setDefaultFileName = Recoil.useState Stores.defaultFileName
    let selectedPath, setSelectedPath = Recoil.useState Stores.selectedPath
    let parsedFiles, setParseFiles = Recoil.useState Stores.parsedFiles
    let i18nJson, setI18nJson = React.useState None
    let parsedFilesRef = React.useRef parsedFiles

    let setI18nFiles = React.useCallbackRef (fun (files: Browser.Types.File list) ->
        setFiles files
        setDefaultFileName None
        setSelectedPath None
        setParseFiles Map.empty
        setI18nJson None
        parsedFilesRef.current <- Map.empty

        for file in files do
            let fileReader = Browser.Dom.FileReader.Create()
            fileReader.onload <- fun e ->
                let map = e.target?result |> Utils.parseToI18nMap
                parsedFilesRef.current <- parsedFilesRef.current |> Map.add file.name map
                setParseFiles parsedFilesRef.current
            fileReader.readAsText(file))


    let setDefaultLocaleFile (fileName: string) =
        let file = files |> List.tryFind (fun x -> x.name = fileName)
        match file with
        | None -> ()
        | Some file ->
            let fileReader = Browser.Dom.FileReader.Create()
            fileReader.onload <- fun e ->
                e.target?result |> SimpleJson.parse |> Some |> setI18nJson
                file.name |> Some |> setDefaultFileName
            fileReader.readAsText(file)


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
                        classes [ Tw.``text-gray-600``; Tw.``font-semibold``; Tw.``text-sm`` ]
                    ]
                    div [
                        classes [ ]
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
                classes [
                    Tw.``ml-4``; Tw.``text-sm``; Tw.``my-1``
                    Tw.``cursor-pointer``; Tw.``px-2``; Tw.``py-1``; Tw.rounded
                    if selectedPath = Some (path </> name) then Tw.``bg-green-600``; Tw.``text-gray-100``; Tw.``shadow-lg``
                    else Tw.``text-gray-600``; Tw.``hover:bg-blue-100``; Tw.``hover:text-gray-800``
                ]
            ]

    div [
        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch``; Tw.``p-2``; Tw.``h-full`` ]
        children [
            FileSelector 
                (i18n.App.Commands.SelectFiles
                ,".json"
                ,setI18nFiles)

            div [
                text i18n.App.Commands.SelectDefaultLocaleFile
                classes [ Tw.``text-gray-400``; Tw.``text-xs``; Tw.``mb-1`` ]
            ]
            select [
                onChange setDefaultLocaleFile
                valueOrDefault (Option.defaultValue "" defaultFileName)
                classes [ Tw.``outline-none``; Tw.``hover:shadow-md``; Tw.``text-yellow-500``; Tw.``bg-yellow-100``; Tw.``p-1``; Tw.rounded ]
                children [
                    for file in files do
                        option [
                            text file.name
                            value file.name
                            classes [ Tw.``appearance-none``; Tw.``p-1``; Tw.``text-gray-600``; Tw.``hover:bg-blue-100`` ]
                        ]
                ]
            ]

            match i18nJson with
            | None -> ()
            | Some json ->
                div [
                    classes [ Tw.``flex-1``; Tw.``overflow-auto``; Tw.``mt-2``; Tw.``p-1``; Tw.rounded; Tw.``bg-gray-100`` ]
                    children [ renderI18nJson "" "" json ]
                ]

            button [
                text i18n.App.Commands.Export
                disabled parsedFilesRef.current.IsEmpty
                classes [ Tw.rounded; Tw.``mt-4``; Tw.``text-center``; Tw.``p-1``; Tw.``hover:bg-blue-100``; Tw.``bg-green-100`` ]
            ]
        ]
    ]
