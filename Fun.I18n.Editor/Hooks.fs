module Fun.I18n.Editor.Hooks

open Feliz.Recoil
open Stores


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
