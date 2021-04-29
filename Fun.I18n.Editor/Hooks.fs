module Fun.I18n.Editor.Hooks

open System
open Feliz
open Feliz.Recoil
open Fable.Core
open Fable.SimpleJson
open Stores


let useLocaleSwitch () =
    let locale, setLocale = Recoil.useState locale
    let setI18n = Recoil.useSetState i18n

    let switch loc =
        if loc <> locale then
            setLocale loc
            match loc with
            | nameof en -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N en)
            | nameof zhcn -> setI18n (Fun.I18n.Provider.Fable.Utils.createI18n I18N zhcn)
            | _ -> ()

    switch


[<Import("saveAs", from="file-saver")>]
let private fileSaverSaveAs data fileName: unit = jsNative

[<Emit("new Blob([$1], {type: $0})")>]
let private createBlobByString (ty: string) (str: string): Browser.Types.Blob = jsNative

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
            |> SimpleJson.toString
            |> createBlobByString ".json"
            |> fun x -> fileSaverSaveAs x fileName)

    save
