module Fun.I18n.Editor.App

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let App () =
    let i18n = Recoil.useValue Stores.i18n

    React.useEffectOnce (fun () ->
        Browser.Dom.document.title <- i18n.App.Title)

    div [
        classes [ Tw.flex; Tw.``flex-row``; Tw.``h-screen`` ]
        children [
            div [
                classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch``; Tw.``w-40``; Tw.``shadow-md`` ]
                children [
                    div [
                        text i18n.App.Title
                        classes [ Tw.``text-center``; Tw.``bg-blue-100``; Tw.``text-blue-500``; Tw.``font-bold``; Tw.``p-4`` ]
                    ]
                    div [
                        classes [ Tw.``flex-1``; Tw.``overflow-auto`` ]
                        children [ I18nFileTree () ]
                    ]
                    LangguageSwitcher ()
                ]
            ]
            div [
                classes [ Tw.``flex-1``; Tw.``overflow-y-auto`` ]
                children [ I18nTranslator () ]
            ]
        ]
    ]


[<ReactComponent>]
let RecoilApp () =
    Recoil.root [
        App()
    ]


ReactDOM.render(RecoilApp(), Browser.Dom.document.getElementById "react-app")
