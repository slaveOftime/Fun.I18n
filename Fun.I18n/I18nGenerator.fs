namespace Fun.I18n


type I18nGenerator =

    /// <summary>
    /// Generate csharp source code for mapping json string to related types
    /// </summary>
    /// <param name="sourceFile">source json file path</param>
    /// <param name="targetDir">the directoy which used to save the generated csharp file</param>
    /// <param name="targetNamespace">the namespace which all generated types should belong to</param>
    static member GenerateForCSharp(sourceFile: string, targetDir: string, targetNamespace: string) =
        CSharp.generateSourceCode sourceFile targetDir targetNamespace

    /// <summary>
    /// Generate csharp source code for mapping json string to related types
    /// </summary>
    /// <param name="sourceFile">source json file path</param>
    /// <param name="targetDir">the directoy which used to save the generated csharp file</param>
    /// <param name="targetNamespace">the namespace which all generated types should belong to</param>
    static member GenerateForFSharp(sourceFile: string, targetDir: string, targetNamespace: string) =
        FSharp.generateSourceCode sourceFile targetDir targetNamespace
