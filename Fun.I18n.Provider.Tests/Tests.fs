module Fun.I18n.Provider.Tests

open System.IO
open Xunit
open Fun.I18n.Provider

let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/test.i18n.json"

type I18N = I18nProvider<TestJsonFilePath>
let i18n = I18N (File.ReadAllText TestJsonFilePath)


[<Fact>]
let ``Formating tests`` () =
    Assert.Equal("String: cool string; Int: 123; Float: 1234; Any: any", i18n.Complex.FormatAll("any", 1234., 123, "cool string"))
    Assert.Equal("This is the coolest app", i18n.App.Title)
    Assert.Equal("You got 1 error", i18n.App.Errors.GotErrors 1)
    Assert.Equal("You got 2 errors", i18n.App.Errors.GotErrors 2)


[<Fact>]
let ``Translation method tests`` () =
    Assert.Equal("This is the coolest app", i18n.Translate("App:Title"))
    Assert.Equal("This is the coolest app", i18n.App.Translate("Title"))
    Assert.Equal("Errors", i18n.Translate("Errors"))
    Assert.Equal("App:Errors", i18n.Translate("App:Errors"))
    Assert.Equal("Errors", i18n.App.Translate("Errors"))
