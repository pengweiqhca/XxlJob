using System.Net.Http.Json;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.WebApi;

public class WebApiContext : IXxlJobContext
{
    private readonly HttpResponseMessage _response;

    public WebApiContext(HttpResponseMessage response) => _response = response;

    public string Method => _response.RequestMessage.RequestUri.AbsolutePath.Split('/')[^1].ToLower();

    public bool TryGetHeader(string headerName, out IReadOnlyList<string> headerValues)
    {
        if (_response.RequestMessage.Headers.TryGetValues(headerName, out var values))
        {
            headerValues = values.ToArray();

            return true;
        }

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
