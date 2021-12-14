using System.Reflection;

namespace XxlJob.Core.Config;

public class JobHandlerOptions
{
    private readonly Dictionary<string, JobHandler> _jobHandlers = new();

    public IReadOnlyDictionary<string, JobHandler> JobHandlers => _jobHandlers;

    public void AddJob<TJob>() where TJob : IJobHandler =>
        AddJob<TJob>(typeof(TJob).GetCustomAttribute<JobHandlerAttribute>()?.Name ?? typeof(TJob).Name);

    public void AddJob<TJob>(string jobName) where TJob : IJobHandler
    {
        if (_jobHandlers.ContainsKey(jobName))
            throw new Exception($"same IJobHandler' name: [{jobName}]");

        _jobHandlers.Add(jobName, new JobHandler(null, typeof(TJob)));
    }

    public void AddJob(IJobHandler job)
    {
        var jobName = job.GetType().GetCustomAttribute<JobHandlerAttribute>()?.Name ?? job.GetType().Name;

        if (_jobHandlers.ContainsKey(jobName))
            throw new Exception($"same IJobHandler' name: [{jobName}]");

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
