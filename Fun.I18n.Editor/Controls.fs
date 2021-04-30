[<AutoOpen>]
module Fun.I18n.Editor.Controls

open Feliz
open Zanaptak.TypedCssClasses
open type Html
open type prop


let [<Literal>] TailwindCssPath = __SOURCE_DIRECTORY__ + "/www/css/tailwind-generated.css"
let [<Literal>] IconmoonCssPath = __SOURCE_DIRECTORY__ + "/www/css/icomoon/style.css"
type Tw = CssClasses<TailwindCssPath, Naming.Verbatim>
type Ic = CssClasses<IconmoonCssPath, Naming.Verbatim>


let spaceV x =
    div [
        classes [
            match x with
            | 1 -> Tw.``h-1``
            | 2 -> Tw.``h-2``
            | 3 -> Tw.``h-3``
            | _ -> Tw.``h-4``
        ]
    ]


[<ReactComponent>]
let FileSelector (label: string, extension: string, onFilesSelected: Browser.Types.File list -> unit) =
    div [
        classes [ 
            Tw.relative; Tw.``py-2``; Tw.rounded; Tw.``bg-white``; Tw.``shadow-md``; Tw.``cursor-pointer``
            Tw.``hover:shadow-lg``; Tw.``hover:bg-blue-100``; Tw.``text-sm``
        ]
        children [
            div [
                text label
                classes [ Tw.``w-full``; Tw.``h-full``; Tw.``text-center`` ]
            ]
            input [
                type' "file"
                onChange onFilesSelected
                accept extension
                multiple true
                classes [ Tw.``opacity-0``; Tw.``z-10``; Tw.absolute; Tw.``top-0``; Tw.``bottom-0``; Tw.``right-0`` ]
            ]
        ]
    ]
