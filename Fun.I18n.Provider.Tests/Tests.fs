module Fun.I18n.Provider.Tests

#if !FABLE_COMPILER
open Expecto
#else
open Fable.Mocha
#endif


let expectedTestJson =
    """
    {
      "App": {
        "Title": "This is the coolest app",
        "Errors": {
          "UserNotFound": "User (%{id}) is not found",
          "GotErrors": "You got %{smart_count} error |||| You got %{smart_count} errors",
          "ErrorMessage": "Error message is %{msg}",
          "ErrorCode": "Error message code is %{code}",
          "ErrorMs": "Error message: time spent (%{time}ms)"
        }
      },
      "Complex": {
        "FormatAll": "String: %{strVar}; Int: %{intVar}; Float: %{floatVar}"
      }
    }
    """

let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/test.i18n.json"


#if !FABLE_COMPILER
type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, false>
let i18n = I18N expectedTestJson
#else
type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, true>
let i18n = Fun.I18n.Provider.Fable.Utils.createI18n I18N expectedTestJson
#endif


let tests =
    testList "Normal tests" [
        testCase "Formating tests" <| fun () ->
            Expect.equal (i18n.Complex.FormatAll("cool string", 123, 1234.)) "String: cool string; Int: 123; Float: 1234" ""
            Expect.equal i18n.App.Title "This is the coolest app"  ""
            Expect.equal (i18n.App.Errors.GotErrors 1) "You got 1 error" ""
            Expect.equal (i18n.App.Errors.GotErrors 2) "You got 2 errors" ""

        testCase "Translation method tests" <| fun () ->
            Expect.equal (i18n.Translate("App:Title")) "This is the coolest app" ""
            Expect.equal (i18n.App.Translate("Title")) "This is the coolest app" ""
            Expect.equal (i18n.Translate("App:Errors")) "App:Errors" ""

        testCase "Should return full path for not translated value" <| fun () ->
            Expect.equal (i18n.App.Save) "App:Save" ""
            Expect.equal (i18n.Translate "App:Save") "App:Save" ""
            Expect.equal (i18n.App.Translate "Save") "App:Save" ""

        testCase "Should return none for not failed tryTranslate" <| fun () ->
            Expect.equal (i18n.TryTranslate "App:Save") None ""
            Expect.equal (i18n.App.TryTranslate "Save") None ""

        testCase "Should override translation correctly" <| fun () ->
            let overrideJson =
                """
                {
                  "App": {
                    "Title": "Overrided"
                  }
                }
                """

            #if !FABLE_COMPILER
            let i18n = Utils.createI18nWith i18n overrideJson
            #else
            let i18n = Fable.Utils.createI18nWith i18n overrideJson
            #endif

            Expect.equal (i18n.App.Title) "Overrided" ""
            Expect.equal (i18n.App.Errors.GotErrors 1) "You got 1 error" ""
            Expect.equal (i18n.Complex.FormatAll("cool string", 123, 1234.)) "String: cool string; Int: 123; Float: 1234" ""
            Expect.equal (i18n.App.Translate "Save") "App:Save" ""
    ]


[<EntryPoint>]
let main args =
#if !FABLE_COMPILER
    runTestsWithArgs defaultConfig args tests
#else
    Mocha.runTests tests
#endif
