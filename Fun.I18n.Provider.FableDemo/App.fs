module Fun.I18n.Provider.FableDemo.App

open Feliz
open type Html


let demoJson = """
{
  "App": {
    "Title": "This is the coolest app",
    "FormatAll": "String: %s{strVar}; Int: %d{intVar}; Float: %f{floatVar}; Any: %{anyVar}"
  }
}
"""


let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/App.i18n.json"

Fun.I18n.Provider.Fable.Utils.setUp()
type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, true>

let i18n = I18N demoJson


[<ReactComponent>]
let App () = 
    div [
        div i18n.App.Title
        div (i18n.App.FormatAll({| data= "any" |}, 123., 123, "fun string"))
        div (i18n.Translate "App:FormatAll")
    ]


ReactDOM.render(App(), Browser.Dom.document.getElementById "react-app")
