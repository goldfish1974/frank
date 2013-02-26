#r "System.Net"
#r "System.Net.Http"
#r "System.Net.Http.Formatting"
#r "System.Web.Http"
#r "System.Web.Http.SelfHost"
#r @"..\packages\Newtonsoft.Json.4.5.10\lib\net40\Newtonsoft.Json.dll"
#load @"..\src\Frank.Net.Http.fs"
#load @"..\src\Frank.Web.Http.Controllers.fs"
#load @"..\src\Frank.Web.Http.Dispatcher.fs"

open System
open System.Net
open System.Net.Http
open System.Web.Http
open System.Web.Http.SelfHost
open Frank.Web.Http.Controllers
open Frank.Web.Http.Dispatcher

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Home =
    let actions: (HttpMethod * HttpApplication) list = [
        (HttpMethod.Get, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "Hello, world!"))
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Contact =
    let actions: (HttpMethod * HttpApplication) list = [
        (HttpMethod.Get, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
        (HttpMethod.Post, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Account =
    let actions: (HttpMethod * HttpApplication) list = [
        (HttpMethod.Get, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Addresses =
    let actions: (HttpMethod * HttpApplication) list = [
        (HttpMethod.Get, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
        (HttpMethod.Post, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
    ]

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Address =
    let actions: (HttpMethod * HttpApplication) list = [
        (HttpMethod.Get, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
        (HttpMethod.Put, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
        (HttpMethod.Delete, fun request -> async.Return <| request.CreateResponse(HttpStatusCode.OK, "<html></html>"))
    ]
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Demo =
    // NOTE: Ultimately, a Resource should be able to use a type as its name and generate its URI
    type RouteName =
        | Home
        | Contacts
        | Account
        | Addresses
        | Address

    let resourceTree =
        Resource("Home", "", Home.actions,
          [| Resource("Contacts", "contacts", Contact.actions)
             Resource("Account", "account", Account.actions,
              [| Resource("Addresses", "addresses", Addresses.actions,
                  [| Resource("Address", "{addressId}", Address.actions) |])
              |])
          |])

let baseUri = "http://127.0.0.1:1000"
let config = new System.Web.Http.SelfHost.HttpSelfHostConfiguration(baseUri)
Resource.route(config, Demo.resourceTree)
let server = new HttpSelfHostServer(config)
server.OpenAsync().Wait()

Console.WriteLine("Running on " + baseUri)
Console.WriteLine("Press any key to stop.")
Console.ReadKey() |> ignore

server.CloseAsync().Wait()
