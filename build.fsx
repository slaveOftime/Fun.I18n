#r "nuget: Fake.Core.Process,5.20.3"
#r "nuget: Fake.IO.FileSystem,5.20.3"
#r "nuget: Fake.DotNet.Cli,5.20.3"
#r "nuget: Fake.JavaScript.Yarn,5.20.3"
#r "nuget: BlackFox.Fake.BuildTask,0.1.3"

#load "./scripts/WebClient.fsx"


open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.JavaScript
open BlackFox.Fake

let editorProjectDir = "Fun.I18n.Editor"
let demoProjectDir = "Fun.I18n.Provider.FableDemo"
let testProjectDir = "Fun.I18n.Provider.Tests"
let publishDir = __SOURCE_DIRECTORY__ </> "publish"
let githubDocs = __SOURCE_DIRECTORY__ </> "docs"


fsi.CommandLineArgs
|> Array.skip 1
|> BuildTask.setupContextFromArgv 


let checkEnv =
    BuildTask.create "CheckEnv" [] {
        Yarn.exec "--version" id
        Yarn.install (fun x -> { x with WorkingDirectory = demoProjectDir </> "www" })
        Yarn.install (fun x -> { x with WorkingDirectory = editorProjectDir </> "www" })
        DotNet.exec id "tool restore" "" |> ignore
    }


let startDemoDev =
    BuildTask.create "StartDemoDev" [ checkEnv ] {
        WebClient.startDev "--exclude Fun.I18n.Provider.fsproj" demoProjectDir 8080
    }

let bundleDemo =
    BuildTask.create "BundleDemo" [ checkEnv ] {
        WebClient.bundle "--exclude Fun.I18n.Provider.fsproj" demoProjectDir (publishDir </> "client")
    }


let startEditorDev =
    BuildTask.create "StartEditorDev" [ checkEnv ] {
        WebClient.startDev "--exclude Fun.I18n.Provider.fsproj" editorProjectDir 8081
    }

let bundleEditor =
    BuildTask.create "BundleEditor" [ checkEnv ] {
        Shell.cleanDir githubDocs
        WebClient.bundle "--exclude Fun.I18n.Provider.fsproj" editorProjectDir githubDocs
    }


let test =
    BuildTask.create "Test" [] {
        DotNet.exec (fun op -> { op with WorkingDirectory = testProjectDir }) "run" "" |> ignore
        Yarn.install (fun op -> { op with WorkingDirectory = testProjectDir })
        Yarn.exec "test" (fun op -> { op with WorkingDirectory = testProjectDir })
    }


BuildTask.runOrDefault test
