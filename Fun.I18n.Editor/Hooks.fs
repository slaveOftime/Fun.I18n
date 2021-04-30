module Fun.I18n.Editor.Hooks

open System
open Feliz
open Feliz.Recoil
open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleJson
open Fun.I18n.Provider.Fable
open Stores


let useLocaleSwitch () =
    let locale, setLocale = Recoil.useState locale
    let setI18n = Recoil.useSetState i18n

    let switch loc =
        if loc <> locale then
            setLocale loc
            match loc with
            | nameof en -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N en)
            | nameof zh_CN -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N zh_CN)
            | _ -> ()

    switch


let useAddParsedFile () =
    let add (parsedFilesRef: IRefValue<Map<string, Map<string, string>>>) (file: Browser.Types.File) =
        let fileReader = Browser.Dom.FileReader.Create()
        fileReader.onload <- fun e ->
            let parsedFile = e.target?result |> Utils.parseToI18nMap
            parsedFilesRef.current <- parsedFilesRef.current |> Map.add file.name parsedFile
        fileReader.readAsText(file)

    add



[<Import("saveAs", from="file-saver")>]
let private fileSaverSaveAs data fileName: unit = jsNative

[<Emit("new Blob([$1], {type: $0})")>]
let private createBlobByString (ty: string) (str: string): Browser.Types.Blob = jsNative

[<Emit("JSON.stringify($1, null, $0)")>]
let private toJsonString (space: string) (data: obj): string = jsNative


let useExportParsedFiles () =
    let parsedFiles = Recoil.useValue Stores.parsedFiles

    let rec createJson (path: string) (parsedFile: List<string * string>) =
        let (</>) str1 str2 =
            if String.IsNullOrEmpty str1 then str2
            elif String.IsNullOrEmpty str2 then str1
            else str1 + ":" + str2

        parsedFile 
        |> List.groupBy (fun (key, _) ->
            let startIndex = if String.IsNullOrEmpty path then 0 else path.Length + 1
            let subKey = key.Substring startIndex
            subKey.Substring(0, subKey.IndexOf ":"))

        |> List.map (fun (name, ls) ->
            let subPath = path </> name
            let ls1, ls2 = ls |> List.partition (fun (key, _) -> key.Substring(subPath.Length + 1).Contains ":" |> not)
            
            let kvs =
                let kvs = ls1 |> List.map (fun (k, v) -> k.Substring(subPath.Length + 1), Json.JString v) |> Map.ofList
                createJson subPath ls2
                |> Map.toList
                |> List.fold
                    (fun state (k, v) -> Map.add k v state)
                    kvs

            name, Json.JObject kvs)

        |> Map.ofList

    let save () =
        parsedFiles
        |> Map.toList
        |> List.iter (fun (fileName, parsedFile) ->
            parsedFile
            |> Map.toList
            |> List.sortBy fst
            |> createJson "" 
            |> Json.JObject
            |> SimpleJson.toPlainObject
            |> toJsonString "  "
            |> createBlobByString ".json"
            |> fun x -> fileSaverSaveAs x fileName)

    save
