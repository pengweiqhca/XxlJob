using Microsoft.Extensions.DependencyInjection;

namespace XxlJob.Core;

public interface IXxlJobBuilder
{
    IServiceCollection Services { get; }
}

internal class XxlJobBuilder : IXxlJobBuilder
{
    public IServiceCollection Services { get; }

    public XxlJobBuilder(IServiceCollection services) => Services = services;
}
