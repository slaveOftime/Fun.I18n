[<AutoOpen>]
module Fun.I18n.Editor.I18nTranslator

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let I18nTranslator () =
    let i18n = Recoil.useValue Stores.i18n
    let defaultLocaleFile = Recoil.useValue Stores.defaultLocaleFile
    let selectedFieldPath = Recoil.useValue Stores.selectedFieldPath
    let parsedFiles = Recoil.useValue Stores.parsedFiles

    div [
        key (string selectedFieldPath)
        classes [ Tw.``p-4``; Tw.``overflow-y-auto`` ]
        children [
            match selectedFieldPath, defaultLocaleFile with
            | Some path, Some defaultFileName ->
                yield div [
                    text path
                    classes [ Tw.``font-semibold``; Tw.``p-2``; Tw.``bg-blue-100``; Tw.rounded ]
                ]

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
