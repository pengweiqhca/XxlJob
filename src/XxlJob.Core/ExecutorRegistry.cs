using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
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
    private readonly XxlJobOptions _options;
    private readonly ILogger<ExecutorRegistry> _logger;
    private TaskCompletionSource<object?>? _tcs;

    public ExecutorRegistry(AdminClient adminClient, IOptions<XxlJobOptions> optionsAccessor, ILogger<ExecutorRegistry> logger)
    {
        if (optionsAccessor == null) throw new ArgumentNullException(nameof(optionsAccessor));

        _adminClient = adminClient;
        _options = optionsAccessor.Value;
        _logger = logger;

        if (!_options.AutoRegistry) _tcs = new TaskCompletionSource<object?>();
    }

    public void BeginRegistry() => Interlocked.Exchange(ref _tcs, null)?.TrySetResult(null);

    public async Task RegistryAsync(CancellationToken cancellationToken)
    {
        //等待执行BeginRegistry;
        if (_tcs?.Task is Task task) await task.ConfigureAwait(false);

        var registryParam = new RegistryParam
        {
            RegistryGroup = "EXECUTOR",
            RegistryKey = _options.AppName,
            RegistryValue = $"http://{_options.IpAddress}:{_options.Port}/{_options.BasePath?.Trim()}",
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
