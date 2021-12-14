using XxlJob.Core.Model;

namespace XxlJob.Core;

public interface IJobHandler
{
    Task<ReturnT> Execute(JobExecuteContext context);
}