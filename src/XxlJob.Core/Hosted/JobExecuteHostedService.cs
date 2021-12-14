using Microsoft.Extensions.Hosting;

namespace XxlJob.Core.Hosted;

/// <summary>
///  NOTE: 负责启动Executor服务，和进行服务注册的宿主服务
/// </summary>
public class JobsExecuteHostedService : BackgroundService
{
    private readonly IExecutorRegistry _registry;

    public JobsExecuteHostedService(IExecutorRegistry registry) => _registry = registry;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _registry.RegistryAsync(stoppingToken);
}
