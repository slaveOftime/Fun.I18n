namespace Fable.Core

type EmitAttribute(macro: string) =
    inherit System.Attribute()


namespace Fun.I18n.Provider

open System.Text.RegularExpressions
open ProviderImplementation.ProvidedTypes

open ProviderDsl


module internal Generator =
    let [<Literal>] SMART_COUNT = "smart_count"
    let [<Literal>] SMART_COUNT_SPLITER = "||||"

    
    [<Fable.Core.Emit("window.$funI18n.parseToMap($0)")>]
    let parseToMap (x: string) = obj()

    [<Fable.Core.Emit("window.$funI18n.translate($0, $1, $2)")>]
    let translate (bundle: Map<string, string>) (path: string) (key: string) = obj()
    
    [<Fable.Core.Emit("window.$funI18n.translateWith($0, $1, $2, $3, $4)")>]
    let translateWith (forSmartCount: bool) (bundle: Map<string, string>) (path: string) (fieldDefs: (string * string) list) (args: obj list) = obj()

    let translateMethod forFable (path: string) =
        Method
            ("Translate", [ "key", ErasedType.String ], ErasedType.String, false
            ,fun args ->
                if forFable then 
                    <@@ translate ((%%args.[0]: obj) :?> Map<string, string>) (%%args.[1]: string) path @@>
                else
                    <@@
                        let bundle = (%%args.[0]: obj) :?> Map<string, string>
                        let key = %%args.[1]: string
                        let path = if path.Length > 0 then path + ":" + key else key
                        bundle
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


    let rec makeMember forFable (path:string) (name, json) =
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
                            if forFable then
                                <@@ translate ((%%args.[0]: obj) :?> Map<string, string>) "" path @@>
                            else
                                <@@
                                    let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                    bundle
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
                            if forFable then
                                let fieldDefs = argFields |> List.map (fun (name, ty) -> name, string ty)
                                let fieldArgs = args |> List.tail |> List.map (fun x -> box x)
                                <@@ translateWith true ((%%args.[0]: obj) :?> Map<string, string>) path fieldDefs fieldArgs @@>
                            else
                                let unformattedValue =
                                    <@
                                        let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                        match Map.tryFind path bundle with
                                        | None -> name
                                        | Some value ->
                                            if value.Contains SMART_COUNT_SPLITER then
                                                let count: int = %%args.[1]
                                                let index = value.IndexOf SMART_COUNT_SPLITER
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
                            if forFable then
                                let fieldDefs = argFields |> List.map (fun (name, ty) -> name, string ty)
                                let fieldArgs = args |> List.tail |> List.map (fun x -> box x)
                                <@@ translateWith true ((%%args.[0]: obj) :?> Map<string, string>) path fieldDefs fieldArgs @@>
                            else
                                let unformattedValue =
                                    <@
                                        let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                        match Map.tryFind path bundle with
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
                        yield! makeMember forFable path memb
                        match memb with
                        | _, JsonParser.Object _ -> translateMethod forFable path
                        | _ -> ()
                    ])
            let nestedType = makeCustomType(name, members)
            [
                ChildType nestedType
                Property(name, Custom nestedType, false, fun args -> <@@ %%args.Head @@>)
            ]


    let createProviderTypeDefinition forFable asm ns typeName sample =
        let makeRootType basicMembers =
            makeRootType(asm, ns, typeName, [
                yield! basicMembers |> List.collect (makeMember forFable "")
                translateMethod forFable ""
                Constructor
                    ([ "jsonString", String ]
                    ,fun args -> 
                        if forFable then <@@ parseToMap %%args.[0] @@>
                        else <@@ JsonParser.parseToMap %%args.[0] @@>)
            ])

        match JsonParser.tryParse sample with
        | Some(JsonParser.Object members) -> makeRootType members |> Some
        | _ -> None
