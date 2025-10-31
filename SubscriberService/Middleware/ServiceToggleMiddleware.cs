using Microsoft.AspNetCore.Http;
using SubscriberService.Features;

namespace SubscriberService.Middleware;

public class ServiceToggleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFeatureToggleService  _featureToggleService;

    public ServiceToggleMiddleware(RequestDelegate next, IFeatureToggleService featureToggleService)
    {
        _next = next;
        _featureToggleService = featureToggleService;
    }

    public async Task Invoke(HttpContext context)
    {
        // The gatekeeper stands eternal, checking a configuration value that determines
        // whether this service participates in the grand charade. A single boolean
        // toggles existence and non-existence, presence and absence, being and void.
        if (!_featureToggleService.IsSubscriberServiceEnabled())
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("SubscriberService is disabled");
            return;
        }
        await _next(context);
    }
}