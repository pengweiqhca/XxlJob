using Microsoft.Extensions.DependencyInjection;
using XxlJob.Core.TaskExecutors;

namespace XxlJob.Core;

/// <summary>
/// 负责响应RPC请求，调度任务执行器的工厂类
/// </summary>
public class TaskExecutorFactory
{
    private readonly Dictionary<string, ITaskExecutor> _cache = new();

    public TaskExecutorFactory(IServiceProvider provider)
    {
        var executors = provider.GetServices<ITaskExecutor>();

        var taskExecutors = executors as ITaskExecutor[] ?? executors.ToArray();
        if (!taskExecutors.Any()) return;

        foreach (var item in taskExecutors)
        {
            _cache.Add(item.GlueType, item);
        }
    }

    public ITaskExecutor? GetTaskExecutor(string? glueType) =>
        glueType == null ? null : _cache.TryGetValue(glueType, out var executor) ? executor : null;
}
