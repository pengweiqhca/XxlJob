using System.Web.Http;
using System.Web.Http.Routing;
using XxlJob.Core;

namespace XxlJob.WebApi;

public static class HttpRouteCollectionExtensions
{
    public static HttpRouteCollection MapXxlJob(this HttpRouteCollection routes) => routes.MapXxlJob("xxl-job");

    public static HttpRouteCollection MapXxlJob(this HttpRouteCollection routes, string? basePath)
    {
        basePath = string.IsNullOrWhiteSpace(basePath) ? null : basePath!.Trim('/') + "/";

        routes.MapHttpRoute("XxlJob", basePath + "{action:xxlJob}", null,
           new { xxlJob = new XxlJobConstraint() }, new XxlJobHandler());

        return routes;
    }

    private static XxlRestfulServiceHandler GetXxlRestfulServiceHandler(HttpRequestMessage request) =>
        request.GetDependencyScope().GetService(typeof(XxlRestfulServiceHandler)) as XxlRestfulServiceHandler ??
        throw new InvalidOperationException("Can't get XxlRestfulServiceHandler instance from WebApi DependencyScope.");

    private class XxlJobHandler : HttpMessageHandler
    {
        public static string Key { get; } = Guid.NewGuid().ToString("N");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            if (request.Properties.TryGetValue(Key, out var p) && p is WebApiContext context)
            {
                response = context.Response;

                request.Properties.Remove(Key);
            }
            else
            {
                response = request.CreateResponse();

                context = new WebApiContext(response)
                {
                    Action = request.GetRouteData()?.Values.TryGetValue("action:xxlJob", out var value) == true ? value?.ToString() ?? string.Empty : string.Empty,
                    HttpMethod = request.Method.Method
                };
            }

            await GetXxlRestfulServiceHandler(request).HandlerAsync(context, cancellationToken).ConfigureAwait(false);

            return response;
        }
    }

    private class XxlJobConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string? parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            parameterName = ":" + parameterName;
            parameterName = values.Keys.FirstOrDefault(key => key.EndsWith(parameterName, StringComparison.Ordinal));
            if (parameterName == null || values[parameterName] is not string action) return false;

            if (request.GetQueryNameValuePairs()
                    .Where(static q => "debug".Equals(q.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(static q => q.Value).FirstOrDefault() is "1" or "true")
                return true;

            var context = new WebApiContext(request.CreateResponse())
            {
                Action = action,
                HttpMethod = request.Method.Method
            };

            request.Properties[XxlJobHandler.Key] = context;

            return GetXxlRestfulServiceHandler(request).IsSupportedRequest(context);
        }
    }
}
