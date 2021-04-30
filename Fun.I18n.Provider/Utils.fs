module Fun.I18n.Provider.Utils


let private overrideTranslation (bundle: Map<string, string>) (newBundle: Map<string, string>) =
    bundle
    |> Map.toList 
    |> List.map fst
    |> List.fold
        (fun state key ->
            newBundle
            |> Map.tryFind key
            |> function
                | None -> state
                | Some v -> state |> Map.add key v)
        bundle


let createI18nWith (defaultI18n: 'I18nProvidedType) (jsonString: string): 'I18nProvidedType =
    let bundle = defaultI18n |> unbox<Map<string, string>>
    let newBundle = JsonParser.parseToMap jsonString
    overrideTranslation bundle newBundle |> unbox<'I18nProvidedType>
