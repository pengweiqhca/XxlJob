using Microsoft.AspNetCore;
using System.Diagnostics;

namespace AspNetCoreExecutor;

public class Program
{
    public static void Main(string[] args)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        using var listener = new ActivityListener();

        ActivitySource.AddActivityListener(listener);

        listener.ShouldListenTo += _ => true;
        listener.Sample += delegate { return ActivitySamplingResult.AllDataAndRecorded; };

        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging((ctx, builder) =>
            {
                builder.AddConfiguration(ctx.Configuration);
                builder.AddConsole();
            })
            .UseStartup<Startup>();
}
