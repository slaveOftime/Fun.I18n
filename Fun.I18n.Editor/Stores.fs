module Fun.I18n.Editor.Stores

open Feliz.Recoil


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
