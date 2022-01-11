using System.Reflection;

namespace XxlJob.Core.Config;

public class JobHandlerOptions
{
    private readonly Dictionary<string, JobHandler> _jobHandlers = new();

    public IReadOnlyDictionary<string, JobHandler> JobHandlers => _jobHandlers;

    public void AddJob<TJob>() where TJob : class, IJobHandler =>
        AddJob(typeof(TJob).GetCustomAttribute<JobHandlerAttribute>()?.Name ?? typeof(TJob).Name, typeof(TJob));

    public void AddJob(Type jobType) =>
        AddJob(jobType.GetCustomAttribute<JobHandlerAttribute>()?.Name ?? jobType.Name, jobType);

    public void AddJob<TJob>(string jobName) where TJob : class, IJobHandler =>
        AddJob(jobName, typeof(TJob));

    public void AddJob(string jobName, Type jobType)
    {
        if (!typeof(IJobHandler).IsAssignableFrom(jobType))
            throw new ArgumentException($"{jobType.FullName} not implement {typeof(IJobHandler).FullName}", nameof(jobType));

        if (jobType.IsAbstract )
            throw new ArgumentException($"{jobType.FullName} should not abstract.", nameof(jobType));

        if (!jobType.IsClass)
            throw new ArgumentException($"{jobType.FullName} must be class.", nameof(jobType));

        if (_jobHandlers.ContainsKey(jobName))
            throw new Exception($"Same IJobHandler' name: [{jobName}]");

        _jobHandlers.Add(jobName, new JobHandler(null, jobType));
    }

    public void AddJob(IJobHandler job)
    {
        var jobName = job.GetType().GetCustomAttribute<JobHandlerAttribute>()?.Name ?? job.GetType().Name;

        if (_jobHandlers.ContainsKey(jobName))
            throw new Exception($"Same IJobHandler' name: [{jobName}], are you register repeatedly?");

        _jobHandlers.Add(jobName, new JobHandler(job, null));
    }

    public readonly struct JobHandler
    {
        public JobHandler(IJobHandler? job, Type? jobType)
        {
            Job = job;
            JobType = jobType;
        }

        public IJobHandler? Job { get; }

        public Type? JobType { get; }
    }
}
