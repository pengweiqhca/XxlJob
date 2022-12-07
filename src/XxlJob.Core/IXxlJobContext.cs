using System.Net;
using XxlJob.Core.Model;

namespace XxlJob.Core;

public interface IXxlJobContext
{
    string HttpMethod { get; }

    string Action { get; }

    bool TryGetHeader(string headerName, out IEnumerable<string> headerValues);

    ValueTask<T?> ReadRequest<T>(CancellationToken cancellationToken);

    ValueTask WriteResponse(HttpStatusCode statusCode, ReturnT ret, CancellationToken cancellationToken);
}
