using Microsoft.Extensions.Options;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Config;

public class XxlJobPostOptions : IPostConfigureOptions<XxlJobOptions>
{
    public void PostConfigure(string name, XxlJobOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.IpAddress))
            options.IpAddress = IpUtility.GetHostIp();
    }
}
