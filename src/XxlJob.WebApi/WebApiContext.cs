using System.Net.Http.Json;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.WebApi;

public class WebApiContext : IXxlJobContext
{
    private readonly HttpResponseMessage _response;

    public WebApiContext(HttpResponseMessage response, string method)
    {
        _response = response;

        Method = method;
    }

    public string Method { get; }

    public bool TryGetHeader(string headerName, out IEnumerable<string> headerValues)
    {
        if (_response.RequestMessage.Headers.TryGetValues(headerName, out headerValues)) return true;

        headerValues = Array.Empty<string>();

        return false;
    }

    public Task<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        _response.RequestMessage.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);

    public ValueTask WriteResponse(ReturnT ret, CancellationToken cancellationToken)
    {
        _response.Content = JsonContent.Create(ret);

        return default;
    }
}
