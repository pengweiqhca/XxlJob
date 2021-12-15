using Microsoft.Extensions.Options;

namespace XxlJob.Core.Config;

public class XxlJobValidateOptions : IValidateOptions<XxlJobOptions>
{
    public ValidateOptionsResult Validate(string name, XxlJobOptions options)
    {
        if (string.IsNullOrEmpty(options.AppName))
            return ValidateOptionsResult.Fail("缺少AppName配置");

        if (options.AdminAddresses is not { Length: > 0 })
            return ValidateOptionsResult.Fail("缺少AdminAddresses配置");

        if (string.IsNullOrEmpty(options.IpAddress))
            return ValidateOptionsResult.Fail("缺少IpAddress配置");

        if (string.IsNullOrEmpty(options.IpAddress) || options.Port < 1)
            return ValidateOptionsResult.Fail("缺少Port配置");

        return ValidateOptionsResult.Success;
    }
}
