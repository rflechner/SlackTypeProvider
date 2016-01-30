(**
- title : SlackTypeProvider 
- description : Introduction to SlackTypeProvider
- author : Romain Flechner
- theme : league
- transition : default

***

### What is SlackTypeProvider?

- A tiny type provider to use Slack API

***

### What is the use?

- Write a little bot sending messages on Slack with an autocomplete on user or channel list.

***

### Is it easy to use ?

*)

(*** hide ***)
#I @"../../packages/Newtonsoft.Json/lib/net40"
#r "Newtonsoft.Json.dll"
#I "../../src/SlackTypeProvider/bin/Debug"
#r "SlackTypeProvider.dll"

open System
open System.IO
open System.Net

(**
See example below

![Image of example1](images/SlackProvider2.gif)

---

*)

open SlackProvider

//You can provide a file path or an URL or a raw token
type TSlack = SlackTypeProvider<token="C:/keys/slack_token.txt">
let slack = TSlack()

slack.Channels.xamarininsights.Send("test")
slack.Users.romain_flechner.Send("Hi !")

// Or with custom image and/or name
slack.Users.romain_flechner
    .Send("I am a bot",
        botname="robot 4", 
        iconUrl="http://statics.romcyber.com/icones/robot1_48x48.jpg")


(**
***
*)
