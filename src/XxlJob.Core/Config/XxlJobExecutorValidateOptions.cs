using Microsoft.Extensions.Options;

namespace XxlJob.Core.Config;

public class XxlJobExecutorValidateOptions : IValidateOptions<XxlJobExecutorOptions>
{
    public ValidateOptionsResult Validate(string name, XxlJobExecutorOptions options)
    {
        if (string.IsNullOrEmpty(options.AdminAddresses))
            return ValidateOptionsResult.Fail("缺少AdminAddresses配置");

        if (string.IsNullOrEmpty(options.SpecialBindUrl) &&
            (string.IsNullOrEmpty(options.SpecialBindAddress) || options.Port < 1))
            return ValidateOptionsResult.Fail("当没有指定SpecialBindUrl时必须配置SpecialBindAddress和有效的Port");

        return ValidateOptionsResult.Success;
    }
}