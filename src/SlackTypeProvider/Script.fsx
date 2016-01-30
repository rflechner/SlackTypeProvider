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

slack.Users.romain_flechner
    .Send("I am a bot",
        botname="robot 4", 
        iconUrl="http://statics.romcyber.com/icones/robot1_48x48.jpg")
