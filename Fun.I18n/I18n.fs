﻿module Fun.I18n.CSharp.I18n

open System.IO
open System.Text.Json
open Fun.I18n.Utils


let generateSourceCode sourceFile targetDir targetNamespace =

    let jsonFile = File.ReadAllText sourceFile
    let rootName = Path.GetFileName(sourceFile).Split(".")[0]
    let rootElement = JsonDocument.Parse(jsonFile).RootElement


    let rec generateCode (deepth: int) (name: string) (element: JsonElement) =
        match element.ValueKind with
        | JsonValueKind.Number -> [
            ""
            $"{indent deepth}private double? _{name};"
            $"{indent deepth}public double {name} => _{name} ??= element?.TryGetProperty(\"{name}\")?.GetDouble() ?? fallbackElement?.TryGetProperty(\"{name}\")?.GetDouble() ?? 0;"
          ]

        | JsonValueKind.String ->
            if formatHoleRegex.IsMatch(element.GetString()) then
                [
                    ""
                    $"{indent deepth}private string _{name};"
                    $"{indent deepth}public string {name}(params object[] objects) => string.Format(_{name} ??= element?.TryGetProperty(\"{name}\")?.GetString() ?? fallbackElement?.TryGetProperty(\"{name}\")?.GetString() ?? \"{name}\", objects);"
                ]
            else
                [
                    ""
                    $"{indent deepth}private string _{name};"
                    $"{indent deepth}public string {name} => _{name} ??= element?.TryGetProperty(\"{name}\")?.GetString() ?? fallbackElement?.TryGetProperty(\"{name}\")?.GetString() ?? \"{name}\";"
                ]

        | JsonValueKind.Object ->
            let list = System.Collections.Generic.List()

            let typePostfix = if deepth > 0 then "Type" else ""

            if deepth > 0 then
                list.Add ""
                list.Add $"{indent deepth}private {name}{typePostfix}? _{name};"
                list.Add
                    $"{indent deepth}public {name}{typePostfix} {name} => _{name} ??= new(element?.TryGetProperty(\"{name}\"), fallbackElement?.TryGetProperty(\"{name}\"));"
                list.Add ""

            list.Add $"{indent deepth}public class {name}{typePostfix} {{"
            list.Add $"{indent deepth}    private readonly JsonElement? element;"
            list.Add $"{indent deepth}    private readonly JsonElement? fallbackElement;"
            list.Add ""
            list.Add
                $"{indent deepth}    public {name}{typePostfix}(JsonElement? ele, JsonElement? fallbackEle) {{ element = ele; fallbackElement = fallbackEle; }}"

            if deepth = 0 then
                list.Add
                    $"""
    /// <summary>
    /// Create a i18n entry
    /// </summary>
    /// <param name="location">location where the i18n json files are located</param>
    /// <param name="defaultCulture">default or fallback culture</param>
    public {name}(string location, CultureInfo defaultCulture) {{
        var name = "{name}";
        element = TryGetCultureJson(location, name, CultureInfo.CurrentUICulture);
        fallbackElement = TryGetCultureJson(location, name, defaultCulture);
    }}

    private JsonElement? TryGetCultureJson(string location, string name, CultureInfo cultureInfo) {{
        var jsonFilePath = Path.Combine(location, name + "." + cultureInfo.Name + ".json");

        if (File.Exists(jsonFilePath)) {{
            return JsonDocument.Parse(File.ReadAllText(jsonFilePath)).RootElement;
        }}
        else {{
            jsonFilePath = Path.Combine(location, name + "." + cultureInfo.TwoLetterISOLanguageName + ".json");
            if (File.Exists(jsonFilePath)) {{
                return JsonDocument.Parse(File.ReadAllText(jsonFilePath)).RootElement;
            }}
        }}

        return null;
    }}
                """

            let mutable obj = element.EnumerateObject()
            while obj.MoveNext() do
                list.AddRange(generateCode (deepth + 1) obj.Current.Name obj.Current.Value)

            list.Add $"{indent deepth}}}"

            Seq.toList list

        | _ -> failwithf "Unsuppported json value %A" element.ValueKind


    let lines = generateCode 0 rootName rootElement

    File.WriteAllLines(
        targetDir </> rootName + ".cs",
        [
            $"""using System.Globalization;
using System.Text.Json;

namespace {targetNamespace};

public static class I18nTypeExtensions {{
    public static JsonElement? TryGetProperty(this JsonElement jsonElement, string propertyName) {{
        if (jsonElement.TryGetProperty(propertyName, out var property)) {{ return property; }}
        return null;
    }}
}}
            """
            yield! lines
        ]
    )
