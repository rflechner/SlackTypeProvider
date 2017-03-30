#I @"../../packages/Newtonsoft.Json/lib/net40"
#r "Newtonsoft.Json.dll"
#I "bin/debug/"
#r "SlackTypeProvider.dll"

open System
open System.IO
open System.Net

open SlackProvider

type TSlack = SlackTypeProvider<token="C:/keys/slack_token.txt">
let slack = TSlack()


// slack.Channels.api_tests
//    .Send("I am a bot",
//        botname="robot 4", 
//        iconUrl="http://statics.romcyber.com/icones/robot1_48x48.jpg")

//let fp = @"C:\dev\SlackTypeProvider\docs\content\images\IHeartFsharp160.png"
//slack.Channels.api_tests.SendFile("FSharp love logo", fp)

slack.Channels.general.SendFile("Ma nouvelle feature", @"C:\Users\rfl\Pictures\gifs\slack_type_provider2.gif")

