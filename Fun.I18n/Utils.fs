module internal Fun.I18n.Utils

open System
open System.IO
open System.Text.RegularExpressions


let (</>) x1 x2 = Path.Combine(x1, x2)

let indent n = String.Concat [| for _ in 1..n -> "    " |]
let formatHoleRegex = Regex "{[\d]+}"
