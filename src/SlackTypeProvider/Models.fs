namespace SlackProvider.Models

open System
open System.Net
open System.Text
open FSharp.Data
open Newtonsoft.Json
open Newtonsoft.Json.Linq

[<CLIMutable>]
type ChannelDescription =
    { [<JsonProperty("id")>] Id:string
      [<JsonProperty("name")>] Name:string
      [<JsonProperty("is_channel")>] IsChannel:bool
      [<JsonProperty("creator")>] Creator:string
      [<JsonProperty("is_archived")>] IsArchived:bool
      [<JsonProperty("is_general")>] IsGeneral:bool
      [<JsonProperty("is_member")>] IsMember:bool
      [<JsonProperty("members")>] MemberIds:string list }

[<CLIMutable>]
type User =
    { [<JsonProperty("id")>] Id:string
      [<JsonProperty("team_id")>] TeamId:string
      [<JsonProperty("name")>] Name:string
      [<JsonProperty("deleted")>] Deleted:bool
      [<JsonProperty("status")>] Status:string
      [<JsonProperty("color")>] Color:string
      [<JsonProperty("real_name")>] RealName:string
      [<JsonProperty("tz")>] Timezone:string
      [<JsonProperty("image_512")>] Avatar:string
      [<JsonProperty("is_admin")>] IsAdmin:bool
      [<JsonProperty("is_restricted")>] IsRestricted:bool
      [<JsonProperty("is_bot")>] IsBot:bool }

[<CLIMutable>]
type AuthResult =
    { [<JsonProperty("ok")>] Success:bool
      [<JsonProperty("url")>] Url:string
      [<JsonProperty("team")>] Team:string
      [<JsonProperty("user")>] User:string
      [<JsonProperty("team_id")>] TeamId:string
      [<JsonProperty("user_id")>] UserId:string }

[<CLIMutable>]
type InstantConversation =
    { [<JsonProperty("id")>] Id:string
      [<JsonProperty("is_im")>] IsIm:bool
      [<JsonProperty("user")>] User:string
      [<JsonProperty("is_user_deleted")>] IsUserDeleted:bool }

type SentMessage =
    { ChannelId:string
      Text:string
      Botname:string option
      AsUser:bool
      IconUrl:string option }

type SlackClient (token) =
    let download (endpoint:string) =
        let sep = if endpoint.Contains "?" then '&' else '?'
        let url = sprintf "%s%ctoken=%s" endpoint sep token
        let client = new WebClient()
        client.DownloadString url
    member private x.downloadJson<'t>(endpoint,path) =
        endpoint |> download |> JObject.Parse
        |> fun o -> o.SelectToken path
        |> fun tk -> tk.ToObject<'t>()
    member x.CheckAuth () =
        x.downloadJson<AuthResult>("https://slack.com/api/auth.test","$")
    member x.GetChannels () =
        x.downloadJson<ChannelDescription list>("https://slack.com/api/channels.list","$.channels")
    member x.GetUsers () =
        x.downloadJson<User list>("https://slack.com/api/users.list","$.members")
    member x.GetUser id =
        x.downloadJson<User>(sprintf "https://slack.com/api/users.info?user=%s" id,"$.user")
    member x.OpenConversation userId =
        x.downloadJson<string>(sprintf "https://slack.com/api/im.open?user=%s" userId, "$.channel.id")
    member x.SendMessage (f:SentMessage->SentMessage) =
        let m = f { ChannelId=""; Text=""; Botname=None; AsUser=false; IconUrl=None }
        let b = StringBuilder "https://slack.com/api/chat.postMessage?token="
        b.Append token |> ignore
        b.Append "&channel=" |> ignore
        m.ChannelId |> Uri.EscapeDataString |> b.Append |> ignore
        b.Append "&text=" |> ignore
        m.Text |> Uri.EscapeDataString |> b.Append |> ignore

        match m.Botname with
        | Some botname ->
            b.Append "&username=" |> ignore
            botname |> Uri.EscapeDataString |> b.Append |> ignore
        | None -> ()

        match m.IconUrl with
        | Some iconurl ->
            b.Append "&icon_url=" |> ignore
            iconurl |> Uri.EscapeDataString |> b.Append |> ignore
        | None -> ()

        b.Append "&as_user=" |> ignore
        m.AsUser.ToString() |> Uri.EscapeDataString |> b.Append |> ignore

        let url = b.ToString()
        x.downloadJson<bool>(url, "$.ok")
