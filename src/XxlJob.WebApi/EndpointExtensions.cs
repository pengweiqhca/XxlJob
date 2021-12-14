using System.Web.Http;
using System.Web.Http.Routing;
using XxlJob.Core;

namespace XxlJob.WebApi;

public static class EndpointExtensions
{
    /// <param name="endpoints"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static IHttpRoute MapXxlJob(this HttpRouteCollection endpoints) =>
        endpoints.MapHttpRoute("XxlJob", "{method:xxlJob}", null,
            new { xxlJob = new XxlJobConstraint() }, new XxlJobHandler());

    private static XxlRestfulServiceHandler GetXxlRestfulServiceHandler(HttpRequestMessage request) =>
        request.GetDependencyScope().GetService(typeof(XxlRestfulServiceHandler)) is XxlRestfulServiceHandler handler
            ? handler
            : throw new InvalidOperationException("不能通过WebApi自带依赖注入获取XxlRestfulServiceHandler实例");

    private class XxlJobHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = request.CreateResponse();

            await GetXxlRestfulServiceHandler(request).HandlerAsync(new WebApiContext(response), cancellationToken).ConfigureAwait(false);

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
