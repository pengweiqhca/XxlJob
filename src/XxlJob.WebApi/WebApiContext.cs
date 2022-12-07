using System.Net;
using System.Net.Http.Json;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.WebApi;

public class WebApiContext : IXxlJobContext
{

    public WebApiContext(HttpResponseMessage response) => Response = response;

    public HttpResponseMessage Response { get; }

    public required string HttpMethod { get; init; }

    public required string Action { get; init; }

    public bool TryGetHeader(string headerName, out IEnumerable<string> headerValues)
    {
        if (Response.RequestMessage.Headers.TryGetValues(headerName, out headerValues)) return true;

        headerValues = Array.Empty<string>();

        return false;
    }

    public ValueTask<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        new(Response.RequestMessage.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken));

    public ValueTask WriteResponse(HttpStatusCode statusCode, ReturnT ret, CancellationToken cancellationToken)
    {
        Response.StatusCode = statusCode;

        Response.Content = JsonContent.Create(ret);

        return default;
    }
}
