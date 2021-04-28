[<AutoOpen>]
module Fun.I18n.Editor.I18nValueEditor

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let I18nValueEditor () =
    let i18n = Recoil.useValue Stores.i18n
    let selectedPath = Recoil.useValue Stores.selectedPath
    let parsedFiles, setParsedFiles = Recoil.useState Stores.parsedFiles

    div [
        children [
            div (parsedFiles.Count)
            match selectedPath with
            | None -> div i18n.App.NoFileSelectedForEdit
            | Some path ->
                yield!
                    parsedFiles
                    |> Map.toList
                    |> List.map (fun (file, map) ->
                        let v = map |> Map.tryFind path |> Option.defaultValue ""
                        div [
                            children [
                                div file
                                input [
                                    value v
                                    onChange (fun (v: string) ->
                                        let map = map |> Map.add path v
                                        parsedFiles |> Map.add file map |> setParsedFiles)
                                ]
                            ]
                        ])
        ]
    ]

