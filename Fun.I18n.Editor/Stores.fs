module Fun.I18n.Editor.Stores

open Feliz.Recoil


let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/App.en.i18n.json"

type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, true>


let en = 
    """
{
"App": {
  "Commands": {
    "AddFile": "Add Files",
    "Export": "Export",
    "SelectDefaultLocaleFile": "Select default locale file",
    "SelectFiles": "Select files & Reset"
  },
  "NoFileSelectedForEdit": "No file selected for edit",
  "NoPathSelectedForEdit": "No field path selected for edit",
  "Title": "I18n json file editor"
}
}
    """

let zh_CN = 
    """
{
"App": {
  "Commands": {
    "AddFile": "添加文件",
    "Export": "导出",
    "SelectDefaultLocaleFile": "选择默认语言文件",
    "SelectFiles": "选择文件 & 重置"
  },
  "NoFileSelectedForEdit": "没有任何文件可供编辑",
  "NoPathSelectedForEdit": "没有选择任何字段以供翻译",
  "Title": "I18n 翻译器"
}
}
    """


let locale = 
    atom {
        key "App.Locale"
        def (nameof en)
    }

let i18n =
    atom {
        key "App.I18n"
        def (Fun.I18n.Provider.Fable.Utils.createI18n I18N en)
    }


let files =
    atom {
        key "App.Files"
        def (List<Browser.Types.File>.Empty)
    }

let parsedFiles =
    atom {
        key "App.ParseFiles"
        def Map.empty<string, Map<string, string>>
    }

let defaultLocaleFile =
    atom {
        key "App.DefaultFileName"
        def Option<string>.None
    }

let selectedFieldPath =
    atom {
        key "App.SelectedPath"
        def Option<string>.None
    }
