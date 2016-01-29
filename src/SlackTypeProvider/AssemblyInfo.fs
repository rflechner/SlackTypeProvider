﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("SlackTypeProvider")>]
[<assembly: AssemblyProductAttribute("SlackTypeProvider")>]
[<assembly: AssemblyDescriptionAttribute("A tiny type provider to use Slack API")>]
[<assembly: AssemblyVersionAttribute("0.0.3")>]
[<assembly: AssemblyFileVersionAttribute("0.0.3")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.3"
