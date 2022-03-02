using XxlJob.Core.Logger;

namespace XxlJob.Core.Model;

public class JobExecuteContext
{
    public JobExecuteContext(IJobLogger jobLogger, string? jobParameter, CancellationToken cancellationToken)
    {
        JobLogger = jobLogger;
        JobParameter = jobParameter;
        CancellationToken = cancellationToken;
    }

    public IJobLogger JobLogger { get; }

    public string? JobParameter { get; }

    public CancellationToken CancellationToken { get; }
}
