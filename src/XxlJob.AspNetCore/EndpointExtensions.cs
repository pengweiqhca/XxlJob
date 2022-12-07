using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XxlJob.Core;
using XxlJob.Core.Config;

namespace XxlJob.AspNetCore;

public static class EndpointExtensions
{
    public static IEndpointConventionBuilder MapXxlJob(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapXxlJob(endpoints.ServiceProvider.GetRequiredService<IOptions<XxlJobOptions>>().Value.BasePath);

    public static IEndpointConventionBuilder MapXxlJob(this IEndpointRouteBuilder endpoints, string? basePath)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

        endpoints.ServiceProvider.GetRequiredService<IOptions<RouteOptions>>().Value
            .ConstraintMap["xxlJob"] = typeof(XxlJobConstraint);

        basePath = string.IsNullOrWhiteSpace(basePath) ? null : basePath.Trim('/') + "/";

        return endpoints.Map(basePath + "{action:xxlJob}", static httpContext =>
        {
            if (httpContext.Items.TryGetValue(typeof(AspNetCoreContext), out var v) && v is AspNetCoreContext context)
                httpContext.Items.Remove(typeof(AspNetCoreContext));
            else
                context = new AspNetCoreContext(httpContext)
                {
                    Action = httpContext.Request.RouteValues.TryGetValue("action", out var value) ? value?.ToString() ?? string.Empty : string.Empty,
                    HttpMethod = httpContext.Request.Method
                };

            return httpContext.RequestServices.GetRequiredService<XxlRestfulServiceHandler>()
                .HandlerAsync(context, httpContext.RequestAborted);
        }).WithDisplayName("XxlJob");
    }

    private class XxlJobConstraint : IRouteConstraint
    {
        private readonly XxlRestfulServiceHandler _handler;

        public XxlJobConstraint(XxlRestfulServiceHandler handler) => _handler = handler;

        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (httpContext == null || !values.TryGetValue(routeKey, out var value) || value is not string action) return false;

            if (httpContext.Request.Query["debug"].FirstOrDefault() is "1" or "true") return true;

            var context = new AspNetCoreContext(httpContext)
            {
                Action = action,
                HttpMethod = httpContext.Request.Method
            };

            httpContext.Items[typeof(AspNetCoreContext)] = context;

            return _handler.IsSupportedRequest(context);
        }
    }
}
