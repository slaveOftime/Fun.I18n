[<AutoOpen>]
module Fun.I18n.Editor.LangguageSwitcher

open Feliz
open Feliz.Recoil

open type Html
open type prop


[<ReactComponent>]
let LangguageSwitcher () =
    let currentLocale = Recoil.useValue Stores.locale
    let localeSwitch = Hooks.useLocaleSwitch ()
    div [
        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-stretch``; Tw.``m-2`` ]
        children [
            button [
                text "中文"
                onClick (fun _ -> localeSwitch (nameof Stores.zhcn))
                classes [ 
                    Tw.rounded; Tw.``bg-gray-100``; Tw.``hover:bg-blue-100``; Tw.``hover:text-gray-600``; Tw.``py-1``; Tw.``text-xs``; Tw.``mb-1``
                    if currentLocale = nameof Stores.zhcn then Tw.``bg-gray-200``; Tw.``text-white``
                ]
            ]
            button [
                text "English"
                onClick (fun _ -> localeSwitch (nameof Stores.en))
                classes [ 
                    Tw.rounded; Tw.``bg-gray-100``; Tw.``hover:bg-blue-100``; Tw.``hover:text-gray-600``; Tw.``py-1``; Tw.``text-xs``
                    if currentLocale = nameof Stores.en then Tw.``bg-gray-400``; Tw.``text-white``
                ]
            ]
        ]
    ]
