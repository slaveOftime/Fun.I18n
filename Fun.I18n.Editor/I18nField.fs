[<AutoOpen>]
module Fun.I18n.Editor.I18nField

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let I18nField (props: {| defaultFilePath: string; filePath: string; path: string |}) =
    let parsedFiles, setParsedFiles = Recoil.useState Stores.parsedFiles
    let parsedFile = parsedFiles |> Map.tryFind props.filePath
    let fieldValue = parsedFile |> Option.bind (Map.tryFind props.path) |> Option.defaultValue ""

    match parsedFile with
    | None -> none
    | Some parsedFile ->
        div [
            classes [ Tw.``bg-gray-100``; Tw.rounded; Tw.``my-4``; Tw.``p-2`` ]
            children [
                div [
                    text props.filePath
                    classes [
                        Tw.``text-sm``
                        if props.defaultFilePath = props.filePath then Tw.``font-semibold``; Tw.``text-yellow-500``
                        else Tw.``text-gray-600``
                    ]
                ]
                input [
                    value fieldValue
                    onChange (fun (v: string) ->
                        let map = parsedFile |> Map.add props.path v
                        parsedFiles |> Map.add props.filePath map |> setParsedFiles)
                    classes [
                        Tw.rounded; Tw.``outline-none``; Tw.rounded; Tw.``px-2``; Tw.``py-1``; Tw.``mt-1``; Tw.``w-full``; Tw.``cursor-pointer``
                        Tw.``hover:bg-blue-100``; Tw.``focus:shadow-lg``; Tw.``focus:border-green-600``; Tw.``border-2``; Tw.``border-transparent``
                    ]
                    autoFocus (if props.defaultFilePath = props.filePath then true else false)
                ]
            ]
        ]
