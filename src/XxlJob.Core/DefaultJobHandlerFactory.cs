using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.Model;

namespace XxlJob.Core;

public class DefaultJobHandlerFactory : IJobHandlerFactory
{
    private readonly JobHandlerOptions _options;

    public DefaultJobHandlerFactory(IOptions<JobHandlerOptions> options, IEnumerable<IJobHandler> jobHandlers)
    {
        _options = options.Value;

        foreach (var handler in jobHandlers) _options.AddJob(handler);
    }

    public IJobHandler? GetJobHandler(IServiceProvider provider, string handlerName)
    {
        if (!_options.JobHandlers.TryGetValue(handlerName, out var jobHandler)) return null;

        if (jobHandler.Job != null) return jobHandler.Job;

        if (jobHandler.JobType == null) return null;

        var job = provider.GetService(jobHandler.JobType);

        if (job is IDisposable) return new JobHandlerWrapper((IJobHandler)job);

        return (IJobHandler)(job ?? ActivatorUtilities.CreateInstance(provider, jobHandler.JobType));
    }

    /// <summary>禁止被用于Dispose</summary>
    private class JobHandlerWrapper : IJobHandler
    {
        private readonly IJobHandler _handler;

        public JobHandlerWrapper(IJobHandler handler) => _handler = handler;

        public Task<ReturnT> Execute(JobExecuteContext context) => _handler.Execute(context);
    }
}
