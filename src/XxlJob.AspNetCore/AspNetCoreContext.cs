using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.AspNetCore;

public class AspNetCoreContext : IXxlJobContext
{
    private readonly HttpContext _context;

    public AspNetCoreContext(HttpContext context) => _context = context;

    public required string HttpMethod { get; init; }

    public required string Action { get; init; }

    public bool TryGetHeader(string headerName, out IEnumerable<string> headerValues)
    {
        if (_context.Request.Headers.TryGetValue(headerName, out var values))
        {
            headerValues = values;

            return true;
        }

        headerValues = Array.Empty<string>();

        return false;
    }
#if NET5_0_OR_GREATER
    public ValueTask<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        _context.Request.ReadFromJsonAsync<T>(cancellationToken);

    public ValueTask WriteResponse(HttpStatusCode statusCode, ReturnT ret, CancellationToken cancellationToken)
    {
        _context.Response.StatusCode = (int)statusCode;

        return new(_context.Response.WriteAsJsonAsync(ret, cancellationToken));
    }
#else
    public ValueTask<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsync<T>(_context.Request.Body, cancellationToken: cancellationToken);

    public ValueTask WriteResponse(HttpStatusCode statusCode, ReturnT ret, CancellationToken cancellationToken)
    {
        _context.Response.ContentType = "application/json;charset=utf-8";
        _context.Response.StatusCode = (int)statusCode;

        return new(JsonSerializer.SerializeAsync(_context.Response.Body, ret, cancellationToken: cancellationToken));
    }
#endif
}
