using System.Threading.Tasks;
using XxlJob.Core;
using XxlJob.Core.Model;

namespace AspNetExecutor;

/// <summary>
/// 示例Job，只是写个日志
/// </summary>
[JobHandler("demoJobHandler")]
public class DemoJobHandler : IJobHandler
{
    public async Task<ReturnT> Execute(JobExecuteContext context)
    {
        context.JobLogger.Log("receive demo job handler,parameter:{0}", context.JobParameter);
        context.JobLogger.Log("开始休眠5秒");
        await Task.Delay(5000, context.CancellationToken).ConfigureAwait(false);
        context.JobLogger.Log("休眠10秒结束");
        return ReturnT.SUCCESS;
    }
}
