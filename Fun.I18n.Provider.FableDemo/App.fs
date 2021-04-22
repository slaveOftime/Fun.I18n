module Fun.I18n.Provider.FableDemo.App

open Feliz
open Feliz.Recoil
open type Html
open type prop


let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/App.i18n.json"

type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, true>


let en = 
    """
    {
      "App": {
        "Title": "This is the coolest app",
        "FormatAll": "String: %{strVar}; Int: %{intVar}; Float: %{floatVar}; Any: %{anyVar}",
        "GotErrors": "You got %{smart_count} error |||| You got %{smart_count} errors"
      }
    }
    """

let zhcn = 
    """
    {
      "App": {
        "Title": "有意思✌",
        "FormatAll": "字符串: %{strVar}; 整型: %{intVar}; 浮点型: %{floatVar}; 任意: %{anyVar}",
        "GotErrors": "有 %{smart_count} 个错误"
      }
    }
    """


let locale = 
    atom {
        key "App.Locale"
        def (nameof en)
    }

let i18n =
    atom {
        key "App.I18n"
        def (Fun.I18n.Provider.Fable.Utils.createI18n I18N en)
    }

let useLocaleSwitch () =
    let locale, setLocale = Recoil.useState locale
    let setI18n = Recoil.useSetState i18n

    let switch loc =
        if loc <> locale then
            setLocale loc
            match loc with
            | nameof en -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N en)
            | nameof zhcn -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N zhcn)
            | _ -> ()

    switch


[<ReactComponent>]
let App () =
    let i18n = Recoil.useValue i18n
    let localeSwitch = useLocaleSwitch ()

    div [
        classes [ Tw.flex; Tw.``flex-col``; Tw.``items-center``; Tw.``justify-center``; Tw.``h-screen`` ]
        children [
            div [
                classes [ Tw.flex; Tw.``flex-row``; Tw.``items-center``; Tw.``mb-4`` ]
                children [
                    button [
                        text "中文"
                        onClick (fun _ -> localeSwitch (nameof zhcn))
                        classes [ Tw.rounded; Tw.``bg-green-100``; Tw.``hover:bg-green-100``; Tw.``mx-2``; Tw.``px-4``; Tw.``py-1`` ]
                    ]
                    button [
                        text "English"
                        onClick (fun _ -> localeSwitch (nameof en))
                        classes [ Tw.rounded; Tw.``bg-green-100``; Tw.``hover:bg-green-100``; Tw.``mx-2``; Tw.``px-4``; Tw.``py-1`` ]
                    ]
                ]
            ]
            div i18n.App.Title
            div (i18n.App.FormatAll("cool string", 123, 1234., "any"))
            div (i18n.Translate "App:FormatAll")
            div (i18n.App.GotErrors 1)
            div (i18n.App.GotErrors 2)
        ]
    ]

[<ReactComponent>]
let RecoilApp () =
    Recoil.root [
        App()
    ]

ReactDOM.render(RecoilApp(), Browser.Dom.document.getElementById "react-app")
