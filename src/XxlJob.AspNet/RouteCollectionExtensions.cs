using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Web;
using System.Web.Routing;
using XxlJob.Core;
using XxlJob.Core.Config;

namespace XxlJob.AspNet;

public static class RouteCollectionExtensions
{
    /// <param name="endpoints"></param>
    /// <param name="requestServices">获取当前请求上下文容器实例（比如Autofac lifetime）</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RouteCollection MapXxlJob(this RouteCollection endpoints,
        Func<HttpContextBase, IServiceProvider> requestServices) =>
        endpoints.MapXxlJob("xxl-job", requestServices);

    /// <param name="endpoints"></param>
    /// <param name="provider">启动时的服务提供器</param>
    /// <param name="requestServices">获取当前请求上下文容器实例（比如Autofac lifetime）</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RouteCollection MapXxlJob(this RouteCollection endpoints,
        IServiceProvider provider,
        Func<HttpContextBase, IServiceProvider> requestServices) =>
        endpoints.MapXxlJob(provider.GetRequiredService<IOptions<XxlJobOptions>>().Value.BasePath, requestServices);

    /// <param name="endpoints"></param>
    /// <param name="basePath"></param>
    /// <param name="requestServices">获取当前请求上下文容器实例（比如Autofac lifetime）</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static RouteCollection MapXxlJob(this RouteCollection endpoints,
        string? basePath,
        Func<HttpContextBase, IServiceProvider> requestServices)
    {
        if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));
        if (requestServices == null) throw new ArgumentNullException(nameof(requestServices));

        basePath = string.IsNullOrWhiteSpace(basePath) ? null : basePath!.Trim('/') + "/";

        endpoints.Add("XxlJob", new Route(basePath + "{method:xxlJob}", null,
            new() { { "xxlJob", new XxlJobConstraint(requestServices) } },
            new XxlJobHandler(requestServices)));

        return endpoints;
    }

    private static XxlRestfulServiceHandler GetXxlRestfulServiceHandler(HttpContextBase httpContext, Func<HttpContextBase, IServiceProvider> requestServices)
    {
        var provider = requestServices(httpContext) ?? throw new InvalidOperationException("requestServices() should not return null.");

        return provider.GetRequiredService<XxlRestfulServiceHandler>();
    }

    private class XxlJobHandler : HttpTaskAsyncHandler, IRouteHandler
    {
        private readonly Func<HttpContextBase, IServiceProvider> _requestServices;

        public XxlJobHandler(Func<HttpContextBase, IServiceProvider> requestServices) => _requestServices = requestServices;

        public IHttpHandler GetHttpHandler(RequestContext requestContext) => this;

        public override async Task ProcessRequestAsync(HttpContext context)
        {
            var method = context.Request.RequestContext.RouteData.Values.TryGetValue("method:xxlJob", out var value) ? value?.ToString() : null;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.Request.TimedOutToken, context.Response.ClientDisconnectedToken);

            await GetXxlRestfulServiceHandler(context.Request.RequestContext.HttpContext, _requestServices)
                .HandlerAsync(new AspNetContext(context, method ?? ""), cts.Token)
                .ConfigureAwait(false);
        }
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
