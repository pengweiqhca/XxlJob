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

        endpoints.Add("XxlJob", new Route(basePath + "{action:xxlJob}", null,
            new() { { "xxlJob", new XxlJobConstraint(requestServices) } },
            new XxlJobHandler(requestServices)));

        return endpoints;
    }

    private static TService GetRequiredService<TService>(HttpContextBase httpContext, Func<HttpContextBase, IServiceProvider> requestServices) where TService : notnull
    {
        var provider = requestServices(httpContext) ?? throw new InvalidOperationException("requestServices() should not return null.");

        return provider.GetRequiredService<TService>();
    }

    private class XxlJobHandler : HttpTaskAsyncHandler, IRouteHandler
    {
        private readonly Func<HttpContextBase, IServiceProvider> _requestServices;

        public XxlJobHandler(Func<HttpContextBase, IServiceProvider> requestServices) => _requestServices = requestServices;

        public IHttpHandler GetHttpHandler(RequestContext requestContext) => this;

        public override async Task ProcessRequestAsync(HttpContext httpContext)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.Request.TimedOutToken, httpContext.Response.ClientDisconnectedToken);

            if (httpContext.Items.Contains(typeof(AspNetContext)) && httpContext.Items[typeof(AspNetContext)] is AspNetContext context)
                httpContext.Items.Remove(typeof(AspNetContext));
            else
                context = new AspNetContext(new HttpContextWrapper(httpContext))
                {
                    Action = httpContext.Request.RequestContext.RouteData.Values.TryGetValue("action:xxlJob", out var value) ? value?.ToString() ?? string.Empty : string.Empty,
                    HttpMethod = httpContext.Request.HttpMethod
                };

            await GetRequiredService<XxlRestfulServiceHandler>(httpContext.Request.RequestContext.HttpContext, _requestServices)
                .HandlerAsync(context, cts.Token).ConfigureAwait(false);
        }
    }

    private class XxlJobConstraint : IRouteConstraint
    {
        private readonly Func<HttpContextBase, IServiceProvider> _requestServices;

        public XxlJobConstraint(Func<HttpContextBase, IServiceProvider> requestServices) => _requestServices = requestServices;

        public bool Match(HttpContextBase? httpContext, Route route, string? parameterName/*not routeKey*/, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (httpContext == null) return false;

            parameterName = ":" + parameterName;
            parameterName = values.Keys.FirstOrDefault(key => key.EndsWith(parameterName, StringComparison.Ordinal));
            if (parameterName == null || values[parameterName] is not string method) return false;

            if (httpContext.Request.QueryString["debug"] is "1" or "true") return true;

            var context = new AspNetContext(httpContext)
            {
                Action = method,
                HttpMethod = httpContext.Request.HttpMethod
            };

            httpContext.Items[typeof(AspNetContext)] = context;

            return GetRequiredService<XxlRestfulServiceHandler>(httpContext, _requestServices).IsSupportedRequest(context);
        }
    }
}
