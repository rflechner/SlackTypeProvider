namespace SlackProvider.Models

open System
open System.Runtime.Caching
open System.Net
open System.Text
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
      [<JsonProperty("members")>] MemberIds:string array }

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

type SentFile =
    { ChannelId:string
      Text:string
      Filepath:string
      Title:string option }
    static member Empty =
      { ChannelId=""; Text=""; Filepath=""; Title=None }

type SlackClient (token) =
    static let cache = new MemoryCache("REST")
    let cacheAndReturns key (f:unit -> 't) =
      if cache.Contains key
      then 
        cache.Get(key) :?> 't
      else
        let result = f()
        let policy = new CacheItemPolicy()
        policy.SlidingExpiration <- TimeSpan.FromMinutes 1.
        cache.Add(key, result, policy) |> ignore
        result

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
        cacheAndReturns "Auth" (fun _ -> x.downloadJson<AuthResult>("https://slack.com/api/auth.test","$"))
    member x.GetChannels () =
        cacheAndReturns "channels" (fun _ -> x.downloadJson<ChannelDescription array>("https://slack.com/api/channels.list","$.channels"))
    member x.GetUsers () =
        cacheAndReturns "users" (fun _ -> x.downloadJson<User array>("https://slack.com/api/users.list","$.members"))
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

    member x.SendFile (f:SentFile->SentFile) =
        let m = f SentFile.Empty
        let b = StringBuilder "https://slack.com/api/files.upload?token="
        b.Append token |> ignore
        b.Append "&channels=" |> ignore
        m.ChannelId |> Uri.EscapeDataString |> b.Append |> ignore
        b.Append "&title=" |> ignore
        m.Text |> Uri.EscapeDataString |> b.Append |> ignore
        b.Append "&filename=" |> ignore
        m.Filepath |> System.IO.Path.GetFileName |> Uri.EscapeDataString |> b.Append |> ignore
        let url = b.ToString()
        let client = new WebClient()
        client.UploadFile(url, m.Filepath) |> ignore

