module Fun.I18n.Provider.FableDemo.App

open Feliz
open type Html
open type prop
open Fable.Core

let demoJson = """
{
  "App": {
    "Title": "This is the coolest app"
  }
}
"""


let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/App.i18n.json"

type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath>

let i18n = I18N demoJson


[<ReactComponent>]
let App () = 
    div [
        div i18n.App.Title
    ]


ReactDOM.render(App(), Browser.Dom.document.getElementById "react-app")
