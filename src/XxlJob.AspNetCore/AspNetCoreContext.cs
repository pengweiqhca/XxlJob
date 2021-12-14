using Microsoft.AspNetCore.Http;
using System.Text.Json;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.AspNetCore;

public class AspNetCoreContext : IXxlJobContext
{
    private readonly HttpContext _context;

    public AspNetCoreContext(HttpContext context) => _context = context;

    public string Method => _context.Request.Path.Value?.Split('/')[^1].ToLower() ?? "";

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
        public Task<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
            _context.Request.ReadFromJsonAsync<T>(cancellationToken).AsTask();

        public ValueTask WriteResponse(ReturnT ret, CancellationToken cancellationToken) =>
            new(_context.Response.WriteAsJsonAsync(ret, cancellationToken));
#else
    public Task<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsync<T>(_context.Request.Body, cancellationToken: cancellationToken).AsTask();

    public ValueTask WriteResponse(ReturnT ret, CancellationToken cancellationToken)
    {
        _context.Response.ContentType = "application/json;charset=utf-8";

        return new(JsonSerializer.SerializeAsync(_context.Response.Body, ret, cancellationToken: cancellationToken));
    }
#endif
}
