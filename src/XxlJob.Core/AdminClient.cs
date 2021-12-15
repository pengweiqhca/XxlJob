using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using XxlJob.Core.Config;
using XxlJob.Core.Internal;
using XxlJob.Core.Model;

namespace XxlJob.Core;

public class AdminClient
{
    private readonly XxlJobOptions _options;
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<AdminClient> _logger;
    private readonly List<AddressEntry> _addresses;
    private int _currentIndex;
    private static readonly string Mapping = "/api";

    public AdminClient(IOptions<XxlJobOptions> optionsAccessor,
        IHttpClientFactory factory,
        ILogger<AdminClient> logger)
    {
        if (optionsAccessor == null) throw new ArgumentNullException(nameof(optionsAccessor));

        _options = optionsAccessor.Value;
        _factory = factory;
        _logger = logger;

        _addresses = new List<AddressEntry>();

        InitAddress();
    }

    private void InitAddress()
    {
        foreach (var item in _options.AdminAddresses)
        {
            try
            {
                var entry = new AddressEntry { RequestUri = item.TrimEnd('/') + Mapping };
                _addresses.Add(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "init admin address error.");
            }
        }
    }

    public Task<ReturnT> Callback(List<HandleCallbackParam> callbackParamList)
    {
        return InvokeRpcService("callback", callbackParamList);
    }

    public Task<ReturnT> Registry(RegistryParam registryParam)
    {
        return InvokeRpcService("registry", registryParam);
    }

    public Task<ReturnT> RegistryRemove(RegistryParam registryParam)
    {
        return InvokeRpcService("registryRemove", registryParam);
    }

    private async Task<ReturnT> InvokeRpcService<T>(string methodName, T jsonObject)
    {
        var triedTimes = 0;
        ReturnT? ret = null;

        while (triedTimes++ < _addresses.Count)
        {
            var address = _addresses[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _addresses.Count;
            if (!address.CheckAccessible()) continue;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{address.RequestUri}/{methodName}");
                if (!string.IsNullOrEmpty(_options.AccessToken))
                    request.Headers.TryAddWithoutValidation("XXL-JOB-ACCESS-TOKEN", _options.AccessToken);

                request.Content = JsonContent.Create(jsonObject);

                using var response = await _factory.CreateClient(Constants.AdminClientName).SendAsync(request).ConfigureAwait(false);
                ret = await response.Content.ReadFromJsonAsync<ReturnT>().ConfigureAwait(false);

                address.Reset();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "request admin error.{0}", ex.Message);

                address.SetFail();
            }
        }

        return ret ?? ReturnT.Failed("call admin fail");
    }
}
