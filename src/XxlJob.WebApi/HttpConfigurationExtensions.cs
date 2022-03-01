using Microsoft.Extensions.Options;
using System.Web.Http;
using XxlJob.Core.Config;

namespace XxlJob.WebApi;

public static class HttpConfigurationExtensions
{
    public static HttpConfiguration UseXxlJob(this HttpConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var options = configuration.DependencyResolver.GetService(typeof(IOptions<XxlJobOptions>)) as IOptions<XxlJobOptions>
                      ?? throw new InvalidOperationException("Can't get IOptions<XxlJobOptions> instance from WebApi DependencyScope.");

        configuration.Routes.MapXxlJob(options.Value.BasePath);

        return configuration;
    }
}
