namespace Fun.I18n.Provider

open System.Text.RegularExpressions
open FSharp.Quotations
open ProviderImplementation.ProvidedTypes

open ProviderDsl
open Fable.Core


type I18nBundle = { JsonData: Map<string, string> }


module internal Generator =
    let [<Literal>] SMART_COUNT = "smart_count"
    let [<Literal>] SMART_COUNT_SPLITER = "||||"

    let callAction prm (x:System.Action<'a>) = x.Invoke(prm)

    // always return root object
    let getterCode name = fun (args: Expr list) -> <@@ %%args.Head @@>


    #if FABLE_COMPILER
    [<Emit("((x,a)=>{let o=(x||{});o[a[0]]=a[1];return o})($1,$0)")>]
    let addkv ((propertyName, value): string * obj) (data: obj = obj()
    #else
    let addkv ((propertyName, value): string * obj) (data: obj) =
        let prop = data.GetType().GetProperty(propertyName)
        prop.SetValue(data, value)
        data
    #endif


    #if FABLE_COMPILER
    [<Emit("$1[$0]")>]
    let getter (propertyName: string) (data: obj) = obj()
    #else
    let getter (propertyName: string) (data: obj) = data.GetType().GetProperty(propertyName).GetValue(data)
    #endif

    #if FABLE_COMPILER
    [<Emit("$2[$0] = $1")>]
    let setter (propertyName: string) (value: obj) (data: obj) = ()
    #else
    let setter (propertyName: string) (value: obj) (data: obj) =
        let prop = data.GetType().GetProperty(propertyName)
        prop.SetValue(data, value)
    #endif


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

            if not hasParameters  then
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
            else
                let argFields =
                    [
                        if hasSmartCount then
                            SMART_COUNT, ErasedType.Int
                        yield!
                            anyParameters
                            |> List.filter (fun x -> x <> SMART_COUNT)
                            |> List.map (fun name -> name, ErasedType.Any)
                        yield! stringParameters |> List.map (fun name -> name, ErasedType.String)
                        yield! intParameters |> List.map (fun name -> name, ErasedType.Int)
                        yield! floatParameters |> List.map (fun name -> name, ErasedType.Float)
                    ]

                [
                    Method
                        (name, argFields, String, false
                        ,fun args ->
                            let replacedString =
                                args
                                |> List.tail
                                |> List.mapi (fun i arg ->
                                    match argFields.[i] with
                                    | name, ErasedType.String   -> "%s{" + name + "}", <@ %%arg: string @>
                                    | name, ErasedType.Int      -> "%d{" + name + "}", <@ (%%arg: int) |> string @>
                                    | name, ErasedType.Float    -> "%f{" + name + "}", <@ (%%arg: float) |> string @>
                                    | name, _                   -> "%{" + name + "}",  <@ (%%arg: obj) |> string @>)
                                |> List.fold
                                    (fun state (name, arg) ->
                                        <@
                                            let state: string = %state
                                            state.Replace(name, %arg)
                                        @>)
                                    <@
                                        let bundle = (%%args.[0]: obj) :?> I18nBundle
                                        match Map.tryFind path bundle.JsonData with
                                        | None -> name
                                        | Some value -> 
                                            if hasMultipleTranslations && hasSmartCount then
                                                if value.Contains SMART_COUNT_SPLITER then
                                                    let index = value.IndexOf SMART_COUNT_SPLITER
                                                    if 1 > 1 then
                                                        value.Substring(index + SMART_COUNT.Length).Trim()
                                                    else
                                                        value.Substring(0, index).Trim()
                                                else
                                                    value
                                            else
                                                value
                                    @>
                            <@@ %replacedString @@>)
                ]

        | JsonParser.Array _ -> []
        | JsonParser.Object members ->
            let members = members |> List.collect (makeMember path)
            let nestedType = makeCustomType(name, members)
            [ ChildType nestedType
              Property(name, Custom nestedType, false, getterCode name) ]



    let createProviderTypeDefinition asm ns typeName sample =
        let makeRootType basicMembers =
            makeRootType(asm, ns, typeName, [
                yield! basicMembers |> List.collect (makeMember "")
                //Method
                //    ("LoadSample", [], ErasedType.Any, true
                //    ,fun args -> <@@  @@>)

                Constructor
                    ([ "jsonString", String ]
                    ,fun args -> <@@ { JsonData = JsonParser.parseToMap %%args.[0] } @@>)
            ])

        match JsonParser.tryParse sample with
        | Some(JsonParser.Object members) -> makeRootType members |> Some
        | _ -> None
