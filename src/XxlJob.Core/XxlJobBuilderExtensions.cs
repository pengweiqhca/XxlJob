using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using XxlJob.Core.Config;
using XxlJob.Core.DefaultHandlers;

namespace XxlJob.Core;

public static class XxlJobBuilderExtensions
{
    public static IXxlJobBuilder AddDefaultXxlJobHandlers(this IXxlJobBuilder builder)
    {
        builder.Services.AddSingleton<IJobHandler, SimpleHttpJobHandler>();

        return builder;
    }

    public static IXxlJobBuilder AddJob<TJob>(this IXxlJobBuilder builder) where TJob : class, IJobHandler
    {
        builder.Services.Configure<JobHandlerOptions>(options => options.AddJob<TJob>()).TryAddScoped<TJob>();

        return builder;
    }

    public static IXxlJobBuilder AddJob<TJob>(this IXxlJobBuilder builder, string jobName) where TJob : class, IJobHandler
    {
        builder.Services.Configure<JobHandlerOptions>(options => options.AddJob<TJob>(jobName)).TryAddScoped<TJob>();

        return builder;
    }

    public static IXxlJobBuilder ScanJob(this IXxlJobBuilder builder, params Assembly[]? assemblies)
    {
        if (assemblies == null || assemblies.Length < 1) return builder;

        foreach (var type in assemblies.SelectMany(assembly =>
        {
            try
            {
                return assembly.DefinedTypes.OfType<Type>();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types;
            }
        }))
        {
            if (!typeof(IJobHandler).IsAssignableFrom(type)) continue;

            var attr = type.GetCustomAttribute<JobHandlerAttribute>();

            string jobName;
            if (attr == null)
            {
                if (!type.IsClass || type.IsAbstract) continue;

                jobName = type.Name;
            }
            else jobName = attr.Name;

            builder.Services.Configure<JobHandlerOptions>(options => options.AddJob(jobName, type)).TryAddScoped(type);
        }

        return builder;
    }
}
