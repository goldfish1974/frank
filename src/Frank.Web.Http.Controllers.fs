(* # Frank Implementation of System.Web.Http.Controllers

## License

Author: Ryan Riley <ryan.riley@panesofglass.org>
Copyright (c) 2011-2012, Ryan Riley.

Licensed under the Apache License, Version 2.0.
See LICENSE.txt for details.
*)
namespace Frank.Web.Http.Controllers

open System
open System.Collections.ObjectModel
open System.Linq
open System.Net.Http
open System.Threading.Tasks
open System.Web.Http.Controllers
open System.Web.Http.ModelBinding

type HttpApplication = HttpRequestMessage -> Async<HttpResponseMessage>

/// All Frank applications take a single "request" parameter of type HttpRequestMessage.
type FrankHttpParameterDescriptor(actionDescriptor) =
    // TODO: Determine if we want to do any model binding or also pass route data params.
    inherit HttpParameterDescriptor(actionDescriptor)
    override x.ParameterName = "request"
    override x.ParameterType = typeof<HttpRequestMessage>
    override x.Prefix = Unchecked.defaultof<_>
    override x.ParameterBinderAttribute
        with get() = Unchecked.defaultof<_>
        and set(v) = ()

/// All Frank actions take the request parameter and return an HttpResponseMessage.
type FrankHttpActionDescriptor(controllerDescriptor, actionName, app: HttpApplication) as x =
    inherit HttpActionDescriptor(controllerDescriptor)
    let parameters = new Collection<HttpParameterDescriptor>([| new FrankHttpParameterDescriptor(x) |])
    override x.ActionName = actionName
    override x.ResultConverter = Unchecked.defaultof<_>
    override x.ReturnType = typeof<HttpResponseMessage>
    override x.GetParameters() = parameters
    override x.ExecuteAsync(controllerContext, arguments, cancellationToken) =
        // TODO: use route data as the arguments?
        // TODO: insert the controller context into the Request.Properties?
        // TODO: just use the controller context?
        // NOTE: You are a fool to use this.
        let runner = async {
            let! value = app controllerContext.Request
            return value :> obj }
        Async.StartAsTask(runner, cancellationToken = cancellationToken)

    member x.AsyncExecute(controllerContext: HttpControllerContext) = app controllerContext.Request


/// The FrankControllerActionSelector pattern matches the HttpMethod and matches to the appropriate handler.
type FrankControllerActionSelector() =
    // TODO: Custom action selector
    // TODO: These are just placeholders; need a custom ControllerDescriptor
    let actionName = "GET"
    let app request = async { return new HttpResponseMessage() }
    interface IHttpActionSelector with
        member x.SelectAction(controllerContext) =
            if controllerContext = null then raise <| ArgumentNullException("controllerContext")
            let controllerDescriptor = controllerContext.ControllerDescriptor
            new FrankHttpActionDescriptor(controllerDescriptor, actionName, app) :> HttpActionDescriptor
        member x.GetActionMapping(controllerDescriptor) =
            if controllerDescriptor = null then raise <| ArgumentNullException("controllerDescriptor")
            // TODO: Get the available actions from the controller descriptor
            let placeholder = new FrankHttpActionDescriptor(controllerDescriptor, actionName, app) :> HttpActionDescriptor
            [| placeholder |].ToLookup((fun (desc: HttpActionDescriptor) -> desc.ActionName), StringComparer.OrdinalIgnoreCase)
