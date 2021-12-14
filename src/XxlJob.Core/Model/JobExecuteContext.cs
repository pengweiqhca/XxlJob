using XxlJob.Core.Logger;

namespace XxlJob.Core.Model;

public class JobExecuteContext
{
    public JobExecuteContext(IJobLogger jobLogger, string? jobParameter)
    {
        JobLogger = jobLogger;
        JobParameter = jobParameter;
    }

    public IJobLogger JobLogger { get; }

    public string? JobParameter { get; }
}
