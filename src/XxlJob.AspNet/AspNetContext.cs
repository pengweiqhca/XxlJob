using System.Net;
using System.Text.Json;
using System.Web;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.AspNet;

public class AspNetContext : IXxlJobContext
{
    private readonly HttpContextBase _context;

    public AspNetContext(HttpContextBase context) => _context = context;

    public required string HttpMethod { get; init; }

    public required string Action { get; init; }

    public bool TryGetHeader(string headerName, out IEnumerable<string> headerValues)
    {
        var values = _context.Request.Headers.GetValues(headerName);
        if (values is { Length: > 0 })
        {
            headerValues = values;

            return true;
        }

        headerValues = Array.Empty<string>();

        return false;
    }

    public ValueTask<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsync<T>(_context.Request.InputStream, cancellationToken: cancellationToken);

    public ValueTask WriteResponse(HttpStatusCode statusCode, ReturnT ret, CancellationToken cancellationToken)
    {
        _context.Response.ContentType = "application/json;charset=utf-8";
        _context.Response.StatusCode = (int)statusCode;

        return new(JsonSerializer.SerializeAsync(_context.Response.OutputStream, ret, cancellationToken: cancellationToken));
    }
}
