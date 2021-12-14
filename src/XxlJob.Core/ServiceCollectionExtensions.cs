using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.DefaultHandlers;
using XxlJob.Core.Hosted;
using XxlJob.Core.Logger;
using XxlJob.Core.Queue;
using XxlJob.Core.TaskExecutors;

namespace XxlJob.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXxlJobExecutor(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.AddOptions();

        services.Configure<XxlJobExecutorOptions>(configuration.GetSection("xxlJob"))
            .AddSingleton<IValidateOptions<XxlJobExecutorOptions>, XxlJobExecutorValidateOptions>()
            .AddXxlJobExecutorServiceDependency();

        return services;
    }
    public static IServiceCollection AddXxlJobExecutor(this IServiceCollection services, Action<XxlJobExecutorOptions> configAction)
    {
        services.AddLogging();
        services.AddOptions();
        services.Configure(configAction).AddXxlJobExecutorServiceDependency();
        return services;
    }

    public static IServiceCollection AddDefaultXxlJobHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IJobHandler, SimpleHttpJobHandler>();

        return services;
    }

    public static IServiceCollection AddJob<TJob>(this IServiceCollection services) where TJob : class, IJobHandler
    {
        services.TryAddScoped<TJob>();

        return services.Configure<JobHandlerOptions>(options => options.AddJob<TJob>());
    }

    public static IServiceCollection AddJob<TJob>(this IServiceCollection services, string jobName) where TJob : class, IJobHandler
    {
        services.TryAddScoped<TJob>();

        return services.Configure<JobHandlerOptions>(options => options.AddJob<TJob>(jobName));
    }

    public static IServiceCollection AddAutoRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IExecutorRegistry, ExecutorRegistry>()
            .AddSingleton<IHostedService, JobsExecuteHostedService>();

        return services;
    }

    private static IServiceCollection AddXxlJobExecutorServiceDependency(this IServiceCollection services)
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

        return services;
    }
}
