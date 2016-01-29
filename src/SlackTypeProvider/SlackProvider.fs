namespace ProviderImplementation

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection
open FSharp.Data
open System
open System.IO
open System.Net
open Newtonsoft.Json
open SlackProvider.Models

[<TypeProvider>]
type SlackTypeProvider () as this =
    inherit TypeProviderForNamespaces ()
    let ns = "SlackProvider"
    let asm = Assembly.GetExecutingAssembly()
    let tyName = "SlackTypeProvider"
    let myType = ProvidedTypeDefinition(asm, ns, tyName, None)
    
    let normalize (name:string) = 
        if name.Contains "."
        then name.Replace('.', '_')
        else name
        
    let readTokenFile path =
        File.ReadAllText(path).Trim()

    let parseToken (tokenOrPath:string) =
        if Uri.CheckSchemeName tokenOrPath
        then
            let uri = Uri tokenOrPath
            match uri.Scheme with
            | "file" -> readTokenFile uri.AbsolutePath
            | "http" ->
                use c = new WebClient()
                (c.DownloadString tokenOrPath).Trim()
            | scheme -> failwithf "not implemented scheme %s" scheme
        elif tokenOrPath.Contains "/" || tokenOrPath.Contains "\\"
        then readTokenFile tokenOrPath
        else tokenOrPath

    do myType.DefineStaticParameters([ProvidedStaticParameter("token", typeof<string>)],
        fun typeName [|:? string as tokenArg|] ->
            let token = parseToken tokenArg
            let ty = ProvidedTypeDefinition(asm, ns, typeName, None)
            let client = SlackClient token

            if client.CheckAuth().Success |> not
            then failwithf "invalid token"

            ProvidedConstructor([], 
                    InvokeCode=(fun _ -> <@@ SlackClient token @@> ))
                |> ty.AddMember

            let channelType = ProvidedTypeDefinition(
                                "ChannelType", 
                                baseType = Some typeof<obj>,
                                HideObjectMethods = true)
            ProvidedConstructor([],
                InvokeCode=
                    fun [c]-> 
                        <@@
                            %%c:ChannelDescription
                        @@>
                ) |> channelType.AddMember
            do ty.AddMember channelType

            let sendMessageMeth = 
                    ProvidedMethod(
                        methodName = "Send", 
                        parameters = [
                            ProvidedParameter("message", typeof<string>)
                            ProvidedParameter("botname", typeof<string>, optionalValue="")
                            ProvidedParameter("asuser", typeof<bool>, optionalValue=false)
                            ProvidedParameter("iconUrl", typeof<string>, optionalValue="")
                            ],
                        returnType = typeof<bool>, 
                        InvokeCode = 
                            fun args -> 
                                <@@ 
                                    let channel = ((%%args.[0]:>obj):?>ChannelDescription)
                                    let message = %%args.[1]:>string
                                    let botname = %%args.[2]:>string
                                    let asuser = %%args.[3]:>bool
                                    let iconUrl = %%args.[4]:>string
                                    SlackClient(token).SendMessage(fun m -> 
                                            { m with 
                                                ChannelId=channel.Id
                                                Text=message
                                                Botname= if String.IsNullOrWhiteSpace botname then None else Some botname
                                                AsUser=asuser
                                                IconUrl=if String.IsNullOrWhiteSpace iconUrl then None else Some iconUrl
                                            })
                                @@>)
            do sendMessageMeth.AddXmlDoc "Send a message to a channel"
            do channelType.AddMember sendMessageMeth

            do ProvidedProperty("Description", typeof<ChannelDescription>,
                  GetterCode=fun args -> 
                    <@@
                        let channel = (%%args.[0]:>obj) :?> ChannelDescription
                        channel
                    @@>
                    ) |> channelType.AddMember

            let channelsType = ProvidedTypeDefinition(
                                "ChannelsType", 
                                baseType = Some typeof<obj>,
                                HideObjectMethods = true)
            ProvidedConstructor([],
                InvokeCode=
                    fun [c]-> 
                        <@@
                            let channels = %%c:ChannelDescription list
                            channels
                        @@>
                ) |> channelsType.AddMember
            do ty.AddMember channelsType

            for channel in client.GetChannels() do
                let id = channel.Id
                ProvidedProperty(normalize channel.Name, channelType,
                  GetterCode=fun args -> 
                    <@@
                        let channels = (%%args.[0]:>obj) :?> ChannelDescription list
                        channels |> Seq.filter (fun c -> c.Id = id) |> Seq.head
                    @@>
                    )
                    |> fun m -> m.AddXmlDoc (sprintf "Get channel with id %s" id); m
                    |> channelsType.AddMember

            let allChannelsMeth = 
                    ProvidedMethod(
                        methodName = "GetAll", 
                        parameters = [],
                        returnType = typeof<ChannelDescription list>, 
                        InvokeCode = 
                            fun args -> 
                                <@@ 
                                    let p = (%%args.[0]:>obj)
                                    let channels = (p:?>ChannelDescription list)
                                    channels
                                @@>)
            do allChannelsMeth.AddXmlDoc "Get all channels as a list"
            do channelsType.AddMember allChannelsMeth

            do ProvidedProperty("Channels", channelsType,
                GetterCode = fun args ->
                    <@@
                        let a = (%%args.[0]:obj) :?> SlackClient
                        a.GetChannels()
                    @@>) |> ty.AddMember

            let userType = ProvidedTypeDefinition(
                                "UserType", 
                                baseType = Some typeof<obj>,
                                HideObjectMethods = true)
            ProvidedConstructor([],
                InvokeCode=
                    fun [c]-> 
                        <@@
                            %%c:User
                        @@>
                ) |> userType.AddMember
            do ty.AddMember userType

            let sendMessageMeth = 
                    ProvidedMethod(
                        methodName = "Send", 
                        parameters = [
                                ProvidedParameter("message", typeof<string>)
                                ProvidedParameter("botname", typeof<string>, optionalValue="")
                                ProvidedParameter("asuser", typeof<bool>, optionalValue=false)
                                ProvidedParameter("iconUrl", typeof<string>, optionalValue="")
                                ],
                        returnType = typeof<bool>, 
                        InvokeCode = 
                            fun args -> 
                                <@@ 
                                    let user = ((%%args.[0]:>obj):?>User)
                                    let message = %%args.[1]:>string
                                    let botname = %%args.[2]:>string
                                    let asuser = %%args.[3]:>bool
                                    let iconUrl = %%args.[4]:>string
                                    let c = SlackClient token
                                    let channelId = c.OpenConversation user.Id
                                    c.SendMessage(fun m -> 
                                            { m with 
                                                ChannelId=channelId
                                                Text=message
                                                Botname= if String.IsNullOrWhiteSpace botname then None else Some botname
                                                AsUser=asuser
                                                IconUrl=if String.IsNullOrWhiteSpace iconUrl then None else Some iconUrl
                                            })
                                @@>)
            do sendMessageMeth.AddXmlDoc "Send a message to a channel"
            do userType.AddMember sendMessageMeth

            do ProvidedProperty("Description", typeof<User>,
                  GetterCode=fun args -> 
                    <@@
                        let user = (%%args.[0]:>obj) :?> User
                        user
                    @@>
                    ) |> userType.AddMember

            let usersType = ProvidedTypeDefinition(
                                "UsersType", 
                                baseType = Some typeof<obj>,
                                HideObjectMethods = true)
            ProvidedConstructor([],
                InvokeCode=
                    fun [c]-> 
                        <@@
                            let users = %%c:User list
                            users
                        @@>
                ) |> usersType.AddMember
            do ty.AddMember usersType

            do ProvidedProperty("Users", usersType,
                GetterCode = fun args ->
                    <@@
                        let a = (%%args.[0]:obj) :?> SlackClient
                        a.GetUsers()
                    @@>) |> ty.AddMember

            for user in client.GetUsers() do
                let id = user.Id
                ProvidedProperty(normalize user.Name, userType,
                  GetterCode=fun args -> 
                    <@@
                        let users = (%%args.[0]:>obj) :?> User list
                        users |> Seq.filter (fun c -> c.Id = id) |> Seq.head
                    @@>
                    )
                |> fun m -> m.AddXmlDoc (sprintf "Get user with id %s" id); m
                |> usersType.AddMember

            let allUsersMeth = 
                    ProvidedMethod(
                        methodName = "GetAll", 
                        parameters = [],
                        returnType = typeof<User list>, 
                        InvokeCode = 
                            fun args -> 
                                <@@ 
                                    let p = (%%args.[0]:>obj)
                                    let users = (p:?>User list)
                                    users
                                @@>)
            do allUsersMeth.AddXmlDoc "Get all users as a list"
            do usersType.AddMember allUsersMeth

            ty)
    do this.AddNamespace(ns, [myType])
    do myType.AddXmlDoc("Get your token at url: https://api.slack.com/tokens")

[<TypeProviderAssembly>]
do ()
