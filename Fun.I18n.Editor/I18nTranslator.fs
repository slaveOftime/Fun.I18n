[<AutoOpen>]
module Fun.I18n.Editor.I18nTranslator

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let I18nTranslator () =
    let i18n = Recoil.useValue Stores.i18n
    let defaultFileName = Recoil.useValue Stores.defaultFileName
    let selectedPath = Recoil.useValue Stores.selectedPath
    let parsedFiles = Recoil.useValue Stores.parsedFiles

    div [
        key (string selectedPath)
        classes [ Tw.``p-4``; Tw.``overflow-y-auto`` ]
        children [
            match selectedPath, defaultFileName with
            | Some path, Some defaultFileName ->
                yield I18nField
                    {| defaultFilePath = defaultFileName
                       filePath = defaultFileName
                       path = path |}

                yield!
                    parsedFiles
                    |> Map.filter (fun k _ -> k <> defaultFileName)
                    |> Map.toList
                    |> List.map (fun (file, _) ->
                        I18nField 
                            {| defaultFilePath = defaultFileName
                               filePath = file
                               path = path |})

            | _, None ->
                yield div [
                    text i18n.App.NoFileSelectedForEdit
                    classes [ Tw.``text-red-600``; Tw.rounded; Tw.``p-2``; Tw.``bg-red-100`` ]
                ]

            | None, _ ->
                yield div [
                    text i18n.App.NoPathSelectedForEdit
                    classes [ Tw.``text-red-600``; Tw.rounded; Tw.``p-2``; Tw.``bg-red-100`` ]
                ]
        ]
    ]

