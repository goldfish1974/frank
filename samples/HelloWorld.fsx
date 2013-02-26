#r "System.Net"
#r "System.Net.Http"
#r "System.Net.Http.Formatting"
#r "System.Web.Http"
#load @"..\src\Frank.Net.Http.fs"
#load @"..\src\Frank.Web.Http.Controllers.fs"
#load @"..\src\Frank.Web.Http.Dispatcher.fs"

open System.Net
open System.Net.Http
open System.Web.Http
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
