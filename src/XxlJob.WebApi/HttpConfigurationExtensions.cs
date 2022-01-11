using Microsoft.Extensions.Options;
using System.Web.Http;
using System.Web.Http.Routing;
using XxlJob.Core;
using XxlJob.Core.Config;

namespace XxlJob.WebApi;

public static class HttpConfigurationExtensions
{
    public static HttpConfiguration UseXxlJob(this HttpConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var options = configuration.DependencyResolver.GetService(typeof(IOptions<XxlJobOptions>)) as IOptions<XxlJobOptions>
                      ?? throw new InvalidOperationException("Can't get IOptions<XxlJobOptions> instance from WebApi DependencyScope.");

        var basePath = string.IsNullOrWhiteSpace(options.Value.BasePath) ? null : options.Value.BasePath!.Trim('/') + "/";

        configuration.Routes.MapHttpRoute("XxlJob", basePath + "{method:xxlJob}", null,
           new { xxlJob = new XxlJobConstraint() }, new XxlJobHandler());

        return configuration;
    }

    private static XxlRestfulServiceHandler GetXxlRestfulServiceHandler(HttpRequestMessage request) =>
        request.GetDependencyScope().GetService(typeof(XxlRestfulServiceHandler)) is XxlRestfulServiceHandler handler
            ? handler
            : throw new InvalidOperationException("Can't get XxlRestfulServiceHandler instance from WebApi DependencyScope.");

    private class XxlJobHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.CreateResponse();

            var method = request.GetRouteData()?.Values.TryGetValue("method:xxlJob", out var value) == true ? value?.ToString() : null;

            await GetXxlRestfulServiceHandler(request).HandlerAsync(new WebApiContext(response, method ?? ""), cancellationToken).ConfigureAwait(false);

            return response;
        }
    }

    private class XxlJobConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string? parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            var contentType = request.Content?.Headers.ContentType.MediaType;

            if (request.Method != HttpMethod.Post || string.IsNullOrEmpty(contentType) ||
                !contentType!.ToLower().StartsWith("application/json")) return false;

            parameterName = ":" + parameterName;
            parameterName = values.Keys.FirstOrDefault(key => key.EndsWith(parameterName));
            if (parameterName == null) return false;

            return values[parameterName] is string method && GetXxlRestfulServiceHandler(request).SupportedMethod(method);
        }
    }
}
