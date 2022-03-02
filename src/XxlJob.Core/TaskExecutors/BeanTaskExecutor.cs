using Microsoft.Extensions.DependencyInjection;
using XxlJob.Core.Internal;
using XxlJob.Core.Logger;
using XxlJob.Core.Model;

namespace XxlJob.Core.TaskExecutors;

/// <summary>
/// 实现 IJobHandler的执行器
/// </summary>
public class BeanTaskExecutor : ITaskExecutor
{
    private readonly IJobHandlerFactory _handlerFactory;
    private readonly IJobLogger _jobLogger;
    private readonly IServiceScopeFactory _factory;

    public BeanTaskExecutor(IJobHandlerFactory handlerFactory, IJobLogger jobLogger, IServiceScopeFactory factory)
    {
        _handlerFactory = handlerFactory;
        _jobLogger = jobLogger;
        _factory = factory;
    }

    public string GlueType => Constants.GlueType.Bean;

    public async Task<ReturnT> Execute(TriggerParam triggerParam, CancellationToken cancellationToken)
    {
        if (triggerParam.ExecutorHandler == null)
            return ReturnT.Failed($"job handler of job {triggerParam.JobId} is null.");

        var scope = _factory.CreateAsyncScope();

        await using var _ = scope.ConfigureAwait(false);

        var handler = _handlerFactory.GetJobHandler(scope.ServiceProvider, triggerParam.ExecutorHandler);

        if (handler == null) return ReturnT.Failed($"job handler [{triggerParam.ExecutorHandler}] not found.");

        try
        {
            return await handler.Execute(new JobExecuteContext(_jobLogger, triggerParam.ExecutorParams, cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            // ReSharper disable SuspiciousTypeConversion.Global
            switch (handler)
            {
                case IAsyncDisposable ad:
                    await ad.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable d:
                    d.Dispose();
                    break;
            }
            // ReSharper restore SuspiciousTypeConversion.Global
        }
    }
}
