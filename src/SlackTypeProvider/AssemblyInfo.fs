namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("SlackTypeProvider")>]
[<assembly: AssemblyProductAttribute("SlackTypeProvider")>]
[<assembly: AssemblyDescriptionAttribute("A tiny type provider to use Slack API")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
