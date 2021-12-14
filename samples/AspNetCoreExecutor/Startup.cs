using XxlJob.AspNetCore;
using XxlJob.Core;

namespace AspNetCoreExecutor;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddXxlJobExecutor(Configuration);
        services.AddDefaultXxlJobHandlers();// add httpHandler;

        services.AddSingleton<IJobHandler, DemoJobHandler>(); // 添加自定义的jobHandler

        services.AddAutoRegistry(); // 自动注册
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting()
            .UseEndpoints(routes => routes.MapXxlJob());
    }
}