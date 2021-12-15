using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Integration.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using XxlJob.Core;
using XxlJob.WebApi;

namespace WebApiExecutor;

public class Global : HttpApplication
{
    private readonly IHost _host;

    public Global() => _host = new HostBuilder()
        .ConfigureAppConfiguration(builder => builder.AddJsonFile(HostingEnvironment.MapPath("~/App_Data/appsettings.json")))
        .ConfigureServices((context, services) => services
            .AddXxlJob(context.Configuration)
            .AddDefaultXxlJobHandlers()
            .ScanJob(typeof(DemoJobHandler).Assembly))
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .Build();

    protected void Application_Start(object sender, EventArgs e)
    {
        _host.Start();

        GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(_host.Services.GetRequiredService<ILifetimeScope>());
        GlobalConfiguration.Configuration.Routes.MapXxlJob();

        Disposed += delegate { _host.Dispose(); };
    }

    protected void Application_End(object sender, EventArgs e)
    {
        _host.StopAsync().GetAwaiter().GetResult();
    }
}
