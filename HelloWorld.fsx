#load @"..\src\Frank.Net.Http.fs"
#load @"..\src\Frank.Net.Http.Controllers.fs"
#load @"..\src\Frank.Net.Http.Dispatcher.fs"

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
        Resource<_>(Home, "", Home.actions,
          [ Resource<_>(Contacts, "contacts", Contact.actions)
            Resource<_>(Account, "account", Account.actions,
              [ Resource<_>(Addresses, "addresses", Addresses.actions,
                  [ Resource<_>(Address, "{addressId}", Address.actions) ])
              ])
          ])
