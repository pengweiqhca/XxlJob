using XxlJob.Core.Model;

namespace XxlJob.Core;

public interface IXxlJobContext
{
    string Method { get; }

    bool TryGetHeader(string headerName, out IReadOnlyList<string> headerValues);

    Task<T?> ReadRequest<T>(CancellationToken cancellationToken);

    ValueTask WriteResponse(ReturnT ret, CancellationToken cancellationToken);
}