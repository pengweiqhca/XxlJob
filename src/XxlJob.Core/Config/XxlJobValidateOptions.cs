using Microsoft.Extensions.Options;

namespace XxlJob.Core.Config;

public class XxlJobValidateOptions : IValidateOptions<XxlJobOptions>
{
    public ValidateOptionsResult Validate(string name, XxlJobOptions options)
    {
        if (string.IsNullOrEmpty(options.AppName))
            return ValidateOptionsResult.Fail("Missing AppName");

        if (options.AdminAddresses is not { Length: > 0 })
            return ValidateOptionsResult.Fail("Missing AdminAddresses");

        if (string.IsNullOrEmpty(options.IpAddress))
            return ValidateOptionsResult.Fail("Missing IpAddress");

        if (string.IsNullOrEmpty(options.IpAddress) || options.Port < 1)
            return ValidateOptionsResult.Fail("Missing Port");

        return ValidateOptionsResult.Success;
    }
}
