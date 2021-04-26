[<AutoOpen>]
module Fun.I18n.Editor.Controls

open Zanaptak.TypedCssClasses

let [<Literal>] TailwindCssPath = __SOURCE_DIRECTORY__ + "/www/css/tailwind-generated.css"
let [<Literal>] IconmoonCssPath = __SOURCE_DIRECTORY__ + "/www/css/icomoon/style.css"
type Tw = CssClasses<TailwindCssPath, Naming.Verbatim>
type Ic = CssClasses<IconmoonCssPath, Naming.Verbatim>
