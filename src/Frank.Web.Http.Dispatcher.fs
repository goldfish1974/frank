(* # Frank Implementation of System.Web.Http.Dispatcher

## License

Author: Ryan Riley <ryan.riley@panesofglass.org>
Copyright (c) 2011-2012, Ryan Riley.

Licensed under the Apache License, Version 2.0.
See LICENSE.txt for details.
*)
namespace Frank.Web.Http.Dispatcher

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Diagnostics.Contracts
open System.Net
open System.Net.Http
open System.Reflection
open System.Threading.Tasks
open System.Web.Http
open System.Web.Http.Controllers
open System.Web.Http.Dispatcher
open System.Web.Http.Filters
open System.Web.Http.Properties
open System.Web.Http.Routing
open Frank.Web.Http.Controllers

// Ultimately, `name` should be able to be a Discriminated Union. However, the generics are tricky at this time.
type Resource(name: string, routeTemplate, actions, ?nestedResources: Resource[]) =
    let nestedResources = defaultArg nestedResources Array.empty
    let groupFilters (filters: Collection<FilterInfo>) =
        if filters <> null && filters.Count > 0 then
            let rec split i (actionFilters, authFilters, exceptionFilters) =
                let result =
                    match filters.[i].Instance with
                    | :? IActionFilter as actionFilter ->
                        actionFilter::actionFilters, authFilters, exceptionFilters
                    | :? IAuthorizationFilter as authFilter ->
                        actionFilters, authFilter::authFilters, exceptionFilters
                    | :? IExceptionFilter as exceptionFilter ->
                        actionFilters, authFilters, exceptionFilter::exceptionFilters
                    | _ -> actionFilters, authFilters, exceptionFilters
                if i < filters.Count then
                    split (i+1) result
                else result
            split 0 ([], [], [])
        else [], [], []
    member x.Name = name
    member x.RouteTemplate = routeTemplate
    member x.Actions = actions
    member x.NestedResources = nestedResources

    static member internal InvokeWithAuthFilters(actionContext, cancellationToken, filters, continuation) =
        Contract.Assert(actionContext <> null)
        List.fold (fun cont (filter: IAuthorizationFilter) ->
            fun () -> filter.ExecuteAuthorizationFilterAsync(actionContext, cancellationToken, Func<_>(cont)))
            continuation
            filters

    static member internal InvokeWithActionFilters(actionContext, cancellationToken, filters, continuation) =
        Contract.Assert(actionContext <> null)
        List.fold (fun cont (filter: IActionFilter) ->
            fun () -> filter.ExecuteActionFilterAsync(actionContext, cancellationToken, Func<_>(cont)))
            continuation
            filters

    interface IHttpController with
        member x.ExecuteAsync(controllerContext, cancellationToken) =
            let controllerDescriptor = controllerContext.ControllerDescriptor
            let services = controllerDescriptor.Configuration.Services
            let actionSelector = services.GetActionSelector()
            let actionDescriptor = actionSelector.SelectAction(controllerContext) :?> FrankHttpActionDescriptor
            let actionContext = new HttpActionContext(controllerContext, actionDescriptor)
            let filters = actionDescriptor.GetFilterPipeline()
            let actionFilters, authFilters, exceptionFilters = groupFilters filters
            let result =
                (Resource.InvokeWithAuthFilters(actionContext, cancellationToken, authFilters, fun () ->
                    // Ignore binding for now.
                    Resource.InvokeWithActionFilters(actionContext, cancellationToken, actionFilters, fun () ->
                        // NOTE: This sucks; make it better
                        Async.StartAsTask(actionDescriptor.AsyncExecute controllerContext, cancellationToken = cancellationToken)
                        // TODO: Need to use Async.Catch to catch exceptions.
                    )()
                )())
            // TODO: attach exception filters; note that this is very bad to do...
            result

type FrankControllerTypeResolver(resource: Resource) =
    let rec aggregate (resource: Resource) =
        [|
            yield resource.GetType()
            if resource.NestedResources |> Seq.isEmpty then () else
            for res in resource.NestedResources do
                yield! aggregate res
        |]
    let types = aggregate resource :> ICollection<_>

    interface IHttpControllerTypeResolver with
        // GetControllerTypes takes an IAssembliesResolver, which is unnecessary in this implementation.
        member x.GetControllerTypes(assembliesResolver) = types

type FrankControllerSelector(configuration: HttpConfiguration, controllerMapping: IDictionary<_,_>) =
    let ControllerKey = "controller"

    member x.GetControllerName (request: HttpRequestMessage) =
        if request = null then raise <| ArgumentNullException("request")
        let routeData = request.GetRouteData()
        if routeData = null then Unchecked.defaultof<_> else
        let success, controllerName = routeData.Values.TryGetValue(ControllerKey)
        controllerName :?> string

    member x.SelectController(request) =
        if request = null then
            raise <| ArgumentNullException("request")

        let controllerName = x.GetControllerName request
        if String.IsNullOrEmpty controllerName then
            raise <| new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.NotFound, request.RequestUri.AbsoluteUri))

        let success, controllerDescriptor = controllerMapping.TryGetValue(controllerName)
        if not success then
            raise <| new HttpResponseException(request.CreateErrorResponse(HttpStatusCode.NotFound, request.RequestUri.AbsoluteUri))

        controllerDescriptor

    interface IHttpControllerSelector with
        member x.GetControllerMapping() = controllerMapping
        member x.SelectController(request) = x.SelectController(request)

type FrankControllerDescriptor(configuration, resource: Resource) =
    inherit HttpControllerDescriptor(configuration, resource.Name, resource.GetType())
    override x.CreateController(request) = resource :> IHttpController
    static member Create(configuration, resource) =
        new FrankControllerDescriptor(configuration, resource) :> HttpControllerDescriptor

type MappedResource = {
    RouteTemplate : string
    Resource : Resource
}

type FrankControllerDispatcher(configuration, resourceMappings: MappedResource[]) =
    inherit HttpMessageHandler()
    do if configuration = null then raise <| new ArgumentNullException("configuration")

    let controllerMapping =
        resourceMappings
        |> Array.map (fun x -> x.Resource.Name, FrankControllerDescriptor.Create(configuration, x.Resource))
        |> dict
    let controllerSelector = new FrankControllerSelector(configuration, controllerMapping)

    member x.Configuration = configuration

    override x.SendAsync(request, cancellationToken) =
        try
            x.InternalSendAsync(request, cancellationToken)
            // TODO: Catch exceptions
        with
        | ex -> // TODO: Handle errors
            base.SendAsync(request, cancellationToken)

    member private x.InternalSendAsync(request, cancellationToken) =
        if request = null then
            raise <| ArgumentNullException("request")
        
        let errorTask message =
            let tcs = new TaskCompletionSource<_>()
            tcs.SetResult(request.CreateErrorResponse(HttpStatusCode.NotFound, "Resource Not Found: " + request.RequestUri.AbsoluteUri, exn(message)))
            tcs.Task
            
        // TODO: Move text into resources.
        let routeData = request.GetRouteData()
        Contract.Assert(routeData <> null)
        let controllerDescriptor = controllerSelector.SelectController(request)
        if controllerDescriptor <> null then errorTask "Resource not selected" else

        let controller = controllerDescriptor.CreateController(request)
        if controller <> null then errorTask "No controller created" else
        // TODO: Appropriately handle other "error" scenarios such as 405 and 406.
        // TODO: Bake in an OPTIONS handler?

        let config = request.GetConfiguration()
        // TODO: Manage the Configuration in the request.Properties

        // Create context
        let controllerContext = new HttpControllerContext(config, routeData, request)
        controllerContext.Controller <- controller
        controllerContext.ControllerDescriptor <- controllerDescriptor
        controller.ExecuteAsync(controllerContext, cancellationToken)

[<System.Runtime.CompilerServices.Extension>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Resource =

    // Merge the current template with the parent path.
    let private makeTemplate parentPath (resource: Resource) =
        if String.IsNullOrWhiteSpace parentPath then
            resource.RouteTemplate
        else parentPath + "/" + resource.RouteTemplate

    [<CompiledName("Flatten")>]
    let flatten (resource: Resource) =
        // This is likely horribly inefficient.
        let rec loop resource parentPath =
            [|
                let template = makeTemplate parentPath resource
                yield { RouteTemplate = template; Resource = resource }
                if resource.NestedResources |> Array.isEmpty then () else
                for nestedResource in resource.NestedResources do
                    yield! loop nestedResource template 
            |]

        if resource.NestedResources |> Array.isEmpty then
            [| { RouteTemplate = resource.RouteTemplate; Resource = resource } |]
        else loop resource ""

    /// Flattens the resource tree, merging route path segments into complete routes.
    [<CompiledName("MapResourceRoute")>]
    [<System.Runtime.CompilerServices.Extension>]
    let route (configuration: HttpConfiguration, resource) =
        let routes = configuration.Routes
        let resourceMappings = flatten resource 
        let dispatcher = new FrankControllerDispatcher(configuration, resourceMappings)
        for mappedResource in resourceMappings do
            // TODO: probably want our own shortcut to allow embedding regex's in the route template.
            routes.MapHttpRoute(
                name = mappedResource.Resource.Name,
                routeTemplate = mappedResource.RouteTemplate,
                defaults = null,
                constraints = null,
                handler = dispatcher) |> ignore

(*
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
*)
