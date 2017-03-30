#I @"../../packages/Newtonsoft.Json/lib/net40"
#r "Newtonsoft.Json.dll"
//#I "bin/debug/"
//#r "SlackTypeProvider.dll"

#r "System.Runtime.Caching"

open System.Runtime
open System.Runtime.Caching

#load "ProvidedTypes.fs"
#load "Models.fs"

open System
open System.IO
open System.Net
open SlackProvider.Models

let token = File.ReadAllText "C:/keys/slack_token.txt"
let client = SlackClient token

client.CheckAuth()
client.GetChannels()

let fp = @"C:\dev\SlackTypeProvider\docs\content\images\IHeartFsharp160.png"
client.SendFile (
  fun f -> 
    { f with Filepath=fp; ChannelId="xxxxxxx"; Text="I love FSHARP" }
  )

