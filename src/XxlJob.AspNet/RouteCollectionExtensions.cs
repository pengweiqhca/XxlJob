using Microsoft.Extensions.DependencyInjection;
using System.Web;
using System.Web.Routing;
using XxlJob.Core;

namespace XxlJob.AspNet;

public static class RouteCollectionExtensions
{
    /// <param name="endpoints"></param>
    /// <param name="requestServices">获取当前请求上下文容器实例（比如Autofac lifetime）</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RouteCollection MapXxlJob(this RouteCollection endpoints, Func<HttpContextBase, IServiceProvider> requestServices)
    {
        if (requestServices == null) throw new ArgumentNullException(nameof(requestServices));

        endpoints.Add("XxlJob", new Route("{method:xxlJob}", null,
            new RouteValueDictionary { { "xxlJob", new XxlJobConstraint(requestServices) } },
            new XxlJobHandler(requestServices)));

        return endpoints;
    }

    private static XxlRestfulServiceHandler GetXxlRestfulServiceHandler(HttpContextBase httpContext, Func<HttpContextBase, IServiceProvider> requestServices)
    {
        var provider = requestServices(httpContext) ?? throw new InvalidOperationException("requestServices不允许返回null");

        return provider.GetRequiredService<XxlRestfulServiceHandler>();
    }

    private class XxlJobHandler : HttpTaskAsyncHandler, IRouteHandler
    {
        private readonly Func<HttpContextBase, IServiceProvider> _requestServices;

        public XxlJobHandler(Func<HttpContextBase, IServiceProvider> requestServices) => _requestServices = requestServices;

        public IHttpHandler GetHttpHandler(RequestContext requestContext) => this;

        public override Task ProcessRequestAsync(HttpContext context) =>
            GetXxlRestfulServiceHandler(new HttpContextWrapper(context), _requestServices)
                .HandlerAsync(new AspNetContext(context), context.Response.ClientDisconnectedToken);
    }

    private class XxlJobConstraint : IRouteConstraint
    {
        private readonly Func<HttpContextBase, IServiceProvider> _requestServices;

        public XxlJobConstraint(Func<HttpContextBase, IServiceProvider> requestServices) => _requestServices = requestServices;

        public bool Match(HttpContextBase? httpContext, Route route, string? parameterName/*not routeKey*/, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (httpContext == null) return false;

            var contentType = httpContext.Request.ContentType;

            if (!"POST".Equals(httpContext.Request.HttpMethod, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(contentType) ||
                !contentType.ToLower().StartsWith("application/json")) return false;

            parameterName = ":" + parameterName;
            parameterName = values.Keys.FirstOrDefault(key => key.EndsWith(parameterName));
            if (parameterName == null) return false;

            return values[parameterName] is string method && GetXxlRestfulServiceHandler(httpContext, _requestServices).SupportedMethod(method);
        }
    }
}
