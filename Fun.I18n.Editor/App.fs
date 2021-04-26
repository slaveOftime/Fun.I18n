module Fun.I18n.Editor.App

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let App () =
    let i18n = Recoil.useValue Stores.i18n
    let currentLocale = Recoil.useValue Stores.locale
    let localeSwitch = Hooks.useLocaleSwitch ()

    div [
        classes [ Tw.flex; Tw.``flex-row``; Tw.``h-screen`` ]
        children [
            div [
                classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch``; Tw.``w-40``; Tw.``bg-gray-100`` ]
                children [
                    div [
                        classes [ Tw.``flex-1`` ]
                        children [ I18nFileTree () ]
                    ]
                    div [
                        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch``; Tw.``m-2`` ]
                        children [
                            button [
                                text "中文"
                                onClick (fun _ -> localeSwitch (nameof Stores.zhcn))
                                classes [ 
                                    Tw.rounded; Tw.``bg-green-100``; Tw.``hover:bg-green-200``; Tw.``py-1``; Tw.``text-sm``; Tw.``mb-2``
                                    if currentLocale = nameof Stores.zhcn then Tw.``bg-green-500``; Tw.``text-white``
                                ]
                            ]
                            button [
                                text "English"
                                onClick (fun _ -> localeSwitch (nameof Stores.en))
                                classes [ 
                                    Tw.rounded; Tw.``bg-green-100``; Tw.``hover:bg-green-200``; Tw.``py-1``; Tw.``text-sm``
                                    if currentLocale = nameof Stores.en then Tw.``bg-green-500``; Tw.``text-white``
                                ]
                            ]
                        ]
                    ]
                ]
            ]
            div [
                classes [ Tw.``flex-1`` ]
                children [ I18nValueEditor () ]
            ]
        ]
    ]


[<ReactComponent>]
let RecoilApp () =
    Recoil.root [
        App()
    ]


ReactDOM.render(RecoilApp(), Browser.Dom.document.getElementById "react-app")
