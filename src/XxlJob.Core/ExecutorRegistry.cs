using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.Internal;
using XxlJob.Core.Model;

namespace XxlJob.Core;

/// <summary>
///  执行器注册注册
/// </summary>
public class ExecutorRegistry : IExecutorRegistry
{
    private readonly AdminClient _adminClient;
    private readonly XxlJobExecutorOptions _options;
    private readonly ILogger<ExecutorRegistry> _logger;

    public ExecutorRegistry(AdminClient adminClient, IOptions<XxlJobExecutorOptions> optionsAccessor, ILogger<ExecutorRegistry> logger)
    {
        if (optionsAccessor == null) throw new ArgumentNullException(nameof(optionsAccessor));

        _adminClient = adminClient;
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task RegistryAsync(CancellationToken cancellationToken)
    {
        var registryParam = new RegistryParam {
            RegistryGroup = "EXECUTOR",
            RegistryKey = _options.AppName,
            RegistryValue = string.IsNullOrEmpty(_options.SpecialBindUrl) ? $"http://{_options.SpecialBindAddress}:{_options.Port}/" : _options.SpecialBindUrl
        };

        _logger.LogInformation(">>>>>>>> start registry to admin <<<<<<<<");

        var errorTimes = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var ret = await _adminClient.Registry(registryParam).ConfigureAwait(false);
                _logger.LogDebug("registry last result:{0}", ret?.Code);
                errorTimes = 0;
                await Task.Delay(Constants.RegistryInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation(">>>>> Application Stopping....<<<<<");
            }
            catch (Exception ex)
            {
                errorTimes++;
                await Task.Delay(Constants.RegistryInterval, cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "registry error:{0},{1} Times", ex.Message, errorTimes);
            }
        }

        _logger.LogInformation(">>>>>>>> end registry to admin <<<<<<<<");

        _logger.LogInformation(">>>>>>>> start remove registry to admin <<<<<<<<");

        var removeRet = await _adminClient.RegistryRemove(registryParam).ConfigureAwait(false);
        _logger.LogInformation("remove registry last result:{0}", removeRet?.Code);
        _logger.LogInformation(">>>>>>>> end remove registry to admin <<<<<<<<");
    }
}
