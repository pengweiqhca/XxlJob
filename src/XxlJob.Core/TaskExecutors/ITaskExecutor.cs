using XxlJob.Core.Model;

namespace XxlJob.Core.TaskExecutors;

public interface ITaskExecutor
{
    string GlueType { get; }

    Task<ReturnT> Execute(TriggerParam triggerParam);
}
