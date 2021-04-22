module Fun.I18n.Provider.FableDemo.App

open Feliz
open type Html


let demoJson = """
{
  "App": {
    "Title": "This is the coolest app",
    "FormatAll": "String: %{strVar}; Int: %{intVar}; Float: %{floatVar}; Any: %{anyVar}",
    "GotErrors": "You got %{smart_count} error |||| You got %{smart_count} errors"
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
        div (i18n.App.FormatAll("cool string", 123, 1234., "any"))
        div (i18n.Translate "App:FormatAll")
        div (i18n.App.GotErrors 1)
        div (i18n.App.GotErrors 2)
    ]


ReactDOM.render(App(), Browser.Dom.document.getElementById "react-app")
