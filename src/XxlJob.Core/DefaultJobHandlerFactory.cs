using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.Model;

namespace XxlJob.Core;

public class DefaultJobHandlerFactory : IJobHandlerFactory
{
    private readonly IServiceProvider _provider;
    private readonly JobHandlerOptions _options;

    public DefaultJobHandlerFactory(IServiceProvider provider, IOptions<JobHandlerOptions> options)
    {
        _provider = provider;
        _options = options.Value;

        Initialize();
    }

    private void Initialize()
    {
        var list = _provider.GetServices<IJobHandler>().ToArray();
        if (list == null || !list.Any())
        {
            throw new TypeLoadException("IJobHandlers are not found in IServiceCollection");
        }

        foreach (var handler in list)
        {
            _options.AddJob(handler);
        }
    }

    public IJobHandler? GetJobHandler(IServiceProvider provider, string handlerName)
    {
        if (!_options.JobHandlers.TryGetValue(handlerName, out var jobHandler)) return null;

        if (jobHandler.Job != null) return jobHandler.Job;

        if (jobHandler.JobType == null) return null;

        var job = provider.GetService(jobHandler.JobType);

        if (job is IDisposable) return new JobHandlerWrapper((IJobHandler)job);

        job ??= ActivatorUtilities.CreateInstance(provider, jobHandler.JobType);

        return (IJobHandler)job;
    }

    /// <summary>禁止被用于Dispose</summary>
    private class JobHandlerWrapper : IJobHandler
    {
        private readonly IJobHandler _handler;

        public JobHandlerWrapper(IJobHandler handler) => _handler = handler;

        public Task<ReturnT> Execute(JobExecuteContext context) => _handler.Execute(context);
    }
}
