namespace Fun.I18n.Provider

open System.Text.RegularExpressions
open ProviderImplementation.ProvidedTypes

open ProviderDsl


type I18nBundle = { JsonData: Map<string, string> }


module internal Generator =
    let [<Literal>] SMART_COUNT = "smart_count"
    let [<Literal>] SMART_COUNT_SPLITER = "||||"


    let translateMethod (path: string) =
        Method
            ("Translate", [ "key", ErasedType.String ], ErasedType.String, false
            ,fun args ->
                <@@
                    let bundle = (%%args.[0]: obj) :?> I18nBundle
                    let key = %%args.[1]: string
                    let path = if path.Length > 0 then path + ":" + key else key
                    bundle.JsonData
                    |> Map.tryFind path
                    |> Option.defaultValue key
                @@>)

    
    let formatWithArgs (fieldDefs: (string * ErasedType) list) args state  =
        args
        |> List.tail
        |> List.mapi (fun i arg ->
            match fieldDefs.[i] with
            | name, _ when name = SMART_COUNT   -> "%{" + SMART_COUNT + "}", <@ (%%arg: int) |> string @>
            | name, ErasedType.String           -> "%s{" + name + "}", <@ %%arg: string @>
            | name, ErasedType.Int              -> "%d{" + name + "}", <@ (%%arg: int) |> string @>
            | name, ErasedType.Float            -> "%f{" + name + "}", <@ (%%arg: float) |> string @>
            | name, _                           -> "%{" + name + "}",  <@ (%%arg: obj) |> string @>)
        |> List.fold
            (fun state (name, arg) ->
                <@
                    let state: string = %state
                    state.Replace(name, %arg)
                @>)
            state


    let rec makeMember (path:string) (name, json) =
        let path = if path.Length > 0 then path + ":" + name else name
        match json with
        | JsonParser.Null -> []
        | JsonParser.Bool _ -> []
        | JsonParser.Number _ -> []
        | JsonParser.String value ->
            let memberName = name

            let searchParameters pattern =
                Regex.Matches(value, pattern, RegexOptions.IgnoreCase)
                |> Seq.cast<System.Text.RegularExpressions.Match>
                |> Seq.map (fun m -> m.Groups.[1].Value )
                |> Seq.distinct
                |> Seq.toList

            let anyParameters = searchParameters "%{(.*?)}"
            let stringParameters = searchParameters "%s{(.*?)}"
            let intParameters = searchParameters "%d{(.*?)}"
            let floatParameters = searchParameters "%f{(.*?)}"

            let hasParameters = anyParameters.Length > 0 || stringParameters.Length > 0 || intParameters.Length > 0 || floatParameters.Length > 0
            let hasMultipleTranslations = value.Contains SMART_COUNT_SPLITER
            let hasSmartCount = anyParameters |> Seq.contains SMART_COUNT


            if not hasParameters then
                [
                    Property
                        (memberName, String, false
                        ,fun args -> 
                            <@@
                                let bundle = (%%args.[0]: obj) :?> I18nBundle
                                bundle.JsonData
                                |> Map.tryFind path
                                |> Option.defaultValue name
                            @@>)
                ]

            elif hasMultipleTranslations && hasSmartCount then
                let argFields =
                    [
                        if hasSmartCount then SMART_COUNT, ErasedType.Int
                        yield!
                            [
                                yield!
                                    anyParameters
                                    |> List.filter (fun x -> x <> SMART_COUNT)
                                    |> List.map (fun name -> name, ErasedType.Any)
                                yield! stringParameters |> List.map (fun name -> name, ErasedType.String)
                                yield! intParameters |> List.map (fun name -> name, ErasedType.Int)
                                yield! floatParameters |> List.map (fun name -> name, ErasedType.Float)
                            ]
                            |> List.sortBy fst
                    ]
                [
                    Method
                        (name, argFields, String, false
                        ,fun args ->
                            let unformattedValue =
                                <@
                                    let bundle = (%%args.[0]: obj) :?> I18nBundle
                                    match Map.tryFind path bundle.JsonData with
                                    | None -> name
                                    | Some value ->
                                        if value.Contains SMART_COUNT_SPLITER then
                                            let index = value.IndexOf SMART_COUNT_SPLITER
                                            let count: int = %%args.[1]
                                            if count = 0 || count = 1 then
                                                value.Substring(0, index).Trim()
                                            else
                                                value.Substring(index + SMART_COUNT_SPLITER.Length).Trim()
                                        else
                                            value
                                @>
                            <@@ %(formatWithArgs argFields args unformattedValue) @@>)
                ]

            else
                let argFields =
                    [
                        yield! anyParameters |> List.map (fun name -> name, ErasedType.Any)
                        yield! stringParameters |> List.map (fun name -> name, ErasedType.String)
                        yield! intParameters |> List.map (fun name -> name, ErasedType.Int)
                        yield! floatParameters |> List.map (fun name -> name, ErasedType.Float)
                    ]
                    |> List.sortBy fst
                [
                    Method
                        (name, argFields, String, false
                        ,fun args ->
                            let unformattedValue =
                                <@
                                    let bundle = (%%args.[0]: obj) :?> I18nBundle
                                    match Map.tryFind path bundle.JsonData with
                                    | None -> name
                                    | Some value -> value
                                @>
                            <@@ %(formatWithArgs argFields args unformattedValue) @@>)
                ]

        | JsonParser.Array _ -> []
        | JsonParser.Object members ->
            let members = 
                members 
                |> List.collect (fun memb ->
                    [
                        yield! makeMember path memb
                        match memb with
                        | _, JsonParser.Object _ -> translateMethod path
                        | _ -> ()
                    ])
            let nestedType = makeCustomType(name, members)
            [
                ChildType nestedType
                Property(name, Custom nestedType, false, fun args -> <@@ %%args.Head @@>)
            ]


    let createProviderTypeDefinition asm ns typeName sample =
        let makeRootType basicMembers =
            makeRootType(asm, ns, typeName, [
                yield! basicMembers |> List.collect (makeMember "")
                translateMethod ""
                Constructor
                    ([ "jsonString", String ]
                    ,fun args -> <@@ { JsonData = Utils.parseToMap %%args.[0] } @@>)
            ])

        match JsonParser.tryParse sample with
        | Some(JsonParser.Object members) -> makeRootType members |> Some
        | _ -> None
