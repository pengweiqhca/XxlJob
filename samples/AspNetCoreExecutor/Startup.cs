using XxlJob.AspNetCore;
using XxlJob.Core;

namespace AspNetCoreExecutor;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration) => _configuration = configuration;

    public void ConfigureServices(IServiceCollection services) => services
        .AddXxlJob(_configuration)
        .AddDefaultXxlJobHandlers()
        .ScanJob(typeof(DemoJobHandler).Assembly);

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.UseRouting()
            .UseEndpoints(routes => routes.MapXxlJob());
    }
}
