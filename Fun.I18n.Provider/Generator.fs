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

    
    [<Fable.Core.Emit("parseToI18nMap($0)")>]
    let parseToMap (x: string) = obj()

    [<Fable.Core.Emit("$0.$i18n.translate($0, $1, $2)")>]
    let translate (bundle: Map<string, string>) (path: string) (key: string) = obj()

    [<Fable.Core.Emit("$0.$i18n.tryTranslate($0, $1, $2)")>]
    let tryTranslate (bundle: Map<string, string>) (path: string) (key: string) = obj()
    
    [<Fable.Core.Emit("$1.$i18n.translateWith($0, $1, $2, $3, $4)")>]
    let translateWith (forSmartCount: bool) (bundle: Map<string, string>) (path: string) (fieldDefs: string list) (args: obj list) = obj()


    let translateMethod forFable (path: string) =
        Method
            ("Translate", [ "key", ErasedType.String ], ErasedType.String, false
            ,fun args ->
                if forFable then
                    <@@ translate ((%%args.[0]: obj) :?> Map<string, string>) path (%%args.[1]: string) @@>
                else
                    <@@
                        let bundle = (%%args.[0]: obj) :?> Map<string, string>
                        let key = %%args.[1]: string
                        let path = if path.Length > 0 then path + ":" + key else key
                        bundle
                        |> Map.tryFind path
                        |> Option.defaultValue path
                    @@>)

    let tryTranslateMethod forFable (path: string) =
        Method
            ("TryTranslate", [ "key", ErasedType.String ], ErasedType.Option ErasedType.String, false
            ,fun args ->
                if forFable then
                    <@@ tryTranslate ((%%args.[0]: obj) :?> Map<string, string>) path (%%args.[1]: string) @@>
                else
                    <@@
                        let bundle = (%%args.[0]: obj) :?> Map<string, string>
                        let key = %%args.[1]: string
                        let path = if path.Length > 0 then path + ":" + key else key
                        bundle
                        |> Map.tryFind path
                    @@>)

    
    let formatWithArgs (fieldDefs: (string * ErasedType) list) args state  =
        args
        |> List.tail
        |> List.mapi (fun i arg ->
            "%{" + fst fieldDefs.[i] + "}"
            ,(match fieldDefs.[i] with
              | SMART_COUNT, ErasedType.Int -> <@ (%%arg: int) |> string @>
              | _ -> <@ (%%arg: obj) |> string @>))
        |> List.fold
            (fun state (name, arg) ->
                <@
                    let state: string = %state
                    state.Replace(name, %arg)
                @>)
            state


    let getMethodsFieldArgs args =
        args 
        |> List.tail 
        |> List.fold 
            (fun state x ->
                <@ 
                    let state = %state: obj list
                    let x = %%x: obj
                    state@[x]
                @>)
            <@ [] @>


    let rec makeMember forFable (path:string) (name, json) =
        let fullPath = if path.Length > 0 then path + ":" + name else name
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

            let hasParameters = anyParameters.Length > 0
            let hasMultipleTranslations = value.Contains SMART_COUNT_SPLITER
            let hasSmartCount = anyParameters |> Seq.contains SMART_COUNT


            if not hasParameters then
                [
                    Property
                        (memberName, String, false
                        ,fun args ->
                            if forFable then
                                <@@ translate ((%%args.[0]: obj) :?> Map<string, string>) "" fullPath @@>
                            else
                                <@@
                                    let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                    bundle
                                    |> Map.tryFind fullPath
                                    |> Option.defaultValue fullPath
                                @@>)
                ]

            else
                let argFields =
                    [
                        if hasSmartCount then
                            if forFable then SMART_COUNT, ErasedType.Any
                            else SMART_COUNT, ErasedType.Int
                        yield!
                            anyParameters
                            |> List.filter (fun x -> x <> SMART_COUNT)
                            |> List.map (fun name -> name, ErasedType.Any)
                    ]
                if hasMultipleTranslations && hasSmartCount then
                    [
                        Method
                            (name, argFields, String, false
                            ,fun args ->
                                if forFable then
                                    let fieldDefs = argFields |> List.map fst
                                    let fieldArgs = getMethodsFieldArgs args
                                    <@@ translateWith true ((%%args.[0]: obj) :?> Map<string, string>) fullPath fieldDefs %fieldArgs @@>
                                else
                                    let unformattedValue =
                                        <@
                                            let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                            match Map.tryFind fullPath bundle with
                                            | None -> name
                                            | Some value ->
                                                if value.Contains SMART_COUNT_SPLITER then
                                                    let count = %%args.[1]: int
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
                    [
                        Method
                            (name, argFields, String, false
                            ,fun args ->
                                if forFable then
                                    let fieldDefs = argFields |> List.map fst
                                    let fieldArgs = getMethodsFieldArgs args
                                    <@@ translateWith true ((%%args.[0]: obj) :?> Map<string, string>) fullPath fieldDefs %fieldArgs @@>
                                else
                                    let unformattedValue =
                                        <@
                                            let bundle = (%%args.[0]: obj) :?> Map<string, string>
                                            match Map.tryFind fullPath bundle with
                                            | None -> name
                                            | Some value -> value
                                        @>
                                    <@@ %(formatWithArgs argFields args unformattedValue) @@>)
                    ]

        | JsonParser.Array _ -> []
        | JsonParser.Object members ->
            let members =
                [
                    yield! members |> List.collect (fun memb -> makeMember forFable fullPath memb)
                    translateMethod forFable fullPath
                    tryTranslateMethod forFable fullPath
                ]
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
                tryTranslateMethod forFable ""
                Constructor
                    ([ "jsonString", String ]
                    ,fun args -> 
                        if forFable then <@@ parseToMap %%args.[0] @@>
                        else <@@ JsonParser.parseToMap %%args.[0] @@>)
            ])

        match JsonParser.tryParse sample with
        | Some(JsonParser.Object members) -> makeRootType members |> Some
        | _ -> None
