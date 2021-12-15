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
    public static IEndpointConventionBuilder MapXxlJob(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

        endpoints.ServiceProvider.GetRequiredService<IOptions<RouteOptions>>().Value
            .ConstraintMap["xxlJob"] = typeof(XxlJobConstraint);

        var basePath = endpoints.ServiceProvider.GetRequiredService<IOptions<XxlJobOptions>>().Value.BasePath;

        basePath = string.IsNullOrWhiteSpace(basePath) ? null : basePath.Trim('/') + "/";

        return endpoints.Map(basePath + "{method:xxlJob}",
                context => context.RequestServices.GetRequiredService<XxlRestfulServiceHandler>()
                    .HandlerAsync(new AspNetCoreContext(context), context.RequestAborted))
            .WithDisplayName("XxlJob");
    }

    private class XxlJobConstraint : IRouteConstraint
    {
        private readonly XxlRestfulServiceHandler _handler;

        public XxlJobConstraint(XxlRestfulServiceHandler handler) => _handler = handler;

        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (httpContext == null) return false;

            var contentType = httpContext.Request.ContentType;

            if (!"POST".Equals(httpContext.Request.Method, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(contentType) ||
                !contentType.ToLower().StartsWith("application/json")) return false;

            return values.TryGetValue(routeKey, out var value) && value is string method && _handler.SupportedMethod(method);
        }
    }
}
