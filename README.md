# Fun.I18n.Provider [![Nuget](https://img.shields.io/nuget/v/Fun.I18n.Provider)](https://www.nuget.org/packages/Fun.I18n.Provider)

This project provide a simple i18n provider which base on the [Titaye](https://github.com/Titaye/Fable.PolyglotProvider) and [Fable.JsonProvider](https://github.com/fable-compiler/Fable.JsonProvider).

The different is I tried to support dotnet and fable at the same time and removed the `node-polyglot` with my own simple implementation.

I also use this project to try fsharp typeprovider and finally learned something.

## How to use

```
dotnet add package Fun.I18n.Provider
```

```fsharp

let [<Literal>] I18nJsonFileTemplate = __SOURCE_DIRECTORY__ + "/test.i18n.json"

#if !FABLE_COMPILER
type I18N = Fun.I18n.Provider.I18nProvider<I18nJsonFileTemplate, false>
#else
Fun.I18n.Provider.Fable.Utils.setUp() // This is required when use fable for pulling some dependencies
type I18N = Fun.I18n.Provider.I18nProvider<I18nJsonFileTemplate, true>
#endif


// You can fetch from server
let translatedI18nJson =
    """
    {
        "App": {
            "Title": "This is the coolest app",
            "FormatAll": "String: %{strVar}; Int: %{intVar}; Float: %{floatVar}; Any: %{anyVar}",
            "GotErrors": "You got %{smart_count} error |||| You got %{smart_count} errors"
        }
    }
    """

let i18n = I18N translatedI18nJson

// Now you are good to go
i18n.App.Title // This is the coolest app
i18n.App.GotErrors(1) // You got 1 error
i18n.App.GotErrors(2) // You got 2 errors
i18n.App.FormatAll("cool string", 123, 1234., "any")

// Or you can try Translate method to get raw translation
i18n.Translate "App:Title" // This is the coolest app
// Or
i18n.App.Translate "Title" // This is the coolest app
```