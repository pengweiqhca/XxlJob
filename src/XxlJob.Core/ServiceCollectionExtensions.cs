using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.Hosted;
using XxlJob.Core.Logger;
using XxlJob.Core.Queue;
using XxlJob.Core.TaskExecutors;

namespace XxlJob.Core;

public static class ServiceCollectionExtensions
{
    public static IXxlJobBuilder AddXxlJob(this IServiceCollection services, IConfiguration configuration) =>
        services.AddXxlJob(configuration.GetSection("xxlJob"));

    public static IXxlJobBuilder AddXxlJob(this IServiceCollection services, IConfigurationSection configuration) =>
        services.Configure<XxlJobExecutorOptions>(configuration)
            .AddSingleton<IValidateOptions<XxlJobExecutorOptions>, XxlJobExecutorValidateOptions>()
            .AddXxlJobCore();

    public static IXxlJobBuilder AddXxlJob(this IServiceCollection services, Action<XxlJobExecutorOptions> configAction) =>
        services.Configure(configAction)
            .AddSingleton<IValidateOptions<XxlJobExecutorOptions>, XxlJobExecutorValidateOptions>()
            .AddXxlJobCore();

    private static IXxlJobBuilder AddXxlJobCore(this IServiceCollection services)
    {
        //可在外部提前注册对应实现，并替换默认实现
        services.TryAddSingleton<IJobLogger, JobLogger>();
        services.TryAddSingleton<IJobHandlerFactory, DefaultJobHandlerFactory>();
        services.TryAddSingleton<IExecutorRegistry, ExecutorRegistry>();

        services.AddHttpClient("XxlJobClient");
        services.TryAddSingleton<JobDispatcher>();
        services.TryAddSingleton<TaskExecutorFactory>();
        services.TryAddSingleton<XxlRestfulServiceHandler>();
        services.TryAddSingleton<CallbackTaskQueue>();
        services.TryAddSingleton<AdminClient>();
        services.TryAddSingleton<ITaskExecutor, BeanTaskExecutor>();

        services.TryAddSingleton<IExecutorRegistry, ExecutorRegistry>();
        services.AddHostedService<JobsExecuteHostedService>();

        return new XxlJobBuilder(services);
    }
}
