using System.Text.Json;
using System.Web;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace XxlJob.AspNet;

public class AspNetContext : IXxlJobContext
{
    private readonly HttpContext _context;

    public AspNetContext(HttpContext context) => _context = context;

    public string Method => _context.Request.Path.Split('/')[^1].ToLower();

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

    public Task<T?> ReadRequest<T>(CancellationToken cancellationToken) =>
        JsonSerializer.DeserializeAsync<T>(_context.Request.InputStream, cancellationToken: cancellationToken).AsTask();

    public ValueTask WriteResponse(ReturnT ret, CancellationToken cancellationToken)
    {
        _context.Response.ContentType = "application/json;charset=utf-8";

        return new (JsonSerializer.SerializeAsync(_context.Response.OutputStream, ret, cancellationToken: cancellationToken));
    }
}
