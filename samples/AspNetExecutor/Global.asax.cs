using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Web;
using System.Web.Hosting;
using System.Web.Routing;
using XxlJob.AspNet;
using XxlJob.Core;

namespace AspNetExecutor;

public class Global : HttpApplication
{
    private static readonly string Key = Guid.NewGuid().ToString("N");

    private readonly IHost _host;

    public Global() => _host = new HostBuilder()
        .ConfigureAppConfiguration(builder => builder.AddJsonFile(HostingEnvironment.MapPath("~/App_Data/appsettings.json")))
        .ConfigureServices((context, services) => services
            .AddXxlJob(context.Configuration)
            .AddDefaultXxlJobHandlers()
            .ScanJob(typeof(DemoJobHandler).Assembly))
        .Build();

    protected void Application_Start(object sender, EventArgs e)
    {
        _host.Start();

        RouteTable.Routes.MapXxlJob(_host.Services, context => (IServiceProvider)context.Items[Key]);

        Disposed += delegate { _host.Dispose(); };
    }

    protected void Application_BeginRequest(object sender, EventArgs e) =>
        Context.Items[Key] = _host.Services.CreateScope();

    protected void Application_EndRequest(object sender, EventArgs e) =>
        (Context.Items[Key] as IDisposable)?.Dispose();

    protected void Application_End(object sender, EventArgs e) =>
        _host.StopAsync().GetAwaiter().GetResult();
}
