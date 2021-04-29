module Fun.I18n.Editor.Stores

open Feliz.Recoil


let [<Literal>] TestJsonFilePath = __SOURCE_DIRECTORY__ + "/App.en.i18n.json"

type I18N = Fun.I18n.Provider.I18nProvider<TestJsonFilePath, true>


let en = 
    """
{
  "App": {
    "Title": "I18n Translator",
    "Commands": {
      "SelectFiles": "Select files",
      "SelectDefaultLocaleFile": "Select default locale file",
      "Export": "Export"
    },
    "NoFileSelectedForEdit": "No file selected for edit",
    "NoPathSelectedForEdit": "No field path selected for edit"
  }
}
    """

let zhcn = 
    """
{
  "App": {
    "Title": "I18n 翻译器",
    "Commands": {
      "SelectFiles": "选择 i18n json 文件",
      "SelectDefaultLocaleFile": "选择默认语言文件",
      "Export": "导出"
    },
    "NoFileSelectedForEdit": "没有任何文件可供编辑",
    "NoPathSelectedForEdit": "没有选择任何字段以供翻译"
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
