[<AutoOpen>]
module Fun.I18n.Editor.I18nFileTree

open Feliz
open Feliz.Recoil
open type Html
open type prop


[<ReactComponent>]
let I18nFileTree () =
    let i18n = Recoil.useValue Stores.i18n
    div [
        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch`` ]
        children [
            div [
                children [
                    span i18n.App.Commands.SelectFiles
                    input [
                        type' "file"
                        multiple true
                        custom ("webkitdirectory", "")
                        classes [ Tw.hidden ]
                    ]
                ]
            ]
            div [
                text i18n.App.Commands.SelectFilesMsg
                classes [ Tw.``text-xs``; Tw.``text-gray-300``; Tw.``bg-yellow-100``; Tw.``text-center`` ]
            ]
        ]
    ]
