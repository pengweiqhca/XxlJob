using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using XxlJob.Core.Internal;
using XxlJob.Core.Json;
using XxlJob.Core.Model;

namespace XxlJob.Core.Queue;

public class RetryCallbackTaskQueue : IDisposable
{
    private readonly Action<HandleCallbackParam> _actionDoCallback;
    private readonly ILogger<RetryCallbackTaskQueue> _logger;

    private CancellationTokenSource? _cancellation;
    private Task? _runTask;
    private readonly string _backupFile;

    public RetryCallbackTaskQueue(string backupPath, Action<HandleCallbackParam> actionDoCallback, ILogger<RetryCallbackTaskQueue> logger)
    {

        _actionDoCallback = actionDoCallback;
        _logger = logger;
        _backupFile = Path.Combine(backupPath, Constants.XxlJobRetryLogsFile);
        var dir = Path.GetDirectoryName(backupPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir ?? throw new("logs path is empty"));
        }

        StartQueue();
    }

    private void StartQueue()
    {
        _cancellation = new();
        var stopToken = _cancellation.Token;

        using var _ = ExecutionContext.SuppressFlow();

        _runTask = Task.Factory.StartNew(async () =>
        {
            while (!stopToken.IsCancellationRequested)
            {
                await LoadFromFile().ConfigureAwait(false);
                await Task.Delay(Constants.CallbackRetryInterval, stopToken).ConfigureAwait(false);
            }

        }, TaskCreationOptions.LongRunning);
    }

    private async Task LoadFromFile()
    {
        var list = new List<HandleCallbackParam>();

        if (!File.Exists(_backupFile))
        {
            return;
        }

        using (var reader = new StreamReader(_backupFile))
        {
            string nextLine;
            while ((nextLine = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                try
                {
                    var item = JsonSerializer.Deserialize<HandleCallbackParam>(nextLine, new JsonSerializerOptions { Converters = { new DateTimeConverter("yyyy-MM-dd HH:mm:ss") } });

                    if (item != null) list.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "read backup file  error:{error}", ex.Message);
                }
            }
        }

        try
        {
            File.Delete(_backupFile); //删除备份文件
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "delete backup file  error:{error}", ex.Message);
        }

        foreach (var item in list)
        {
            _actionDoCallback(item);
        }
    }

    public void Push(List<HandleCallbackParam> list)
    {
        if (list.Count == 0)
        {
            return;
        }

        try
        {
            using var writer = new StreamWriter(_backupFile, true, Encoding.UTF8);

            foreach (var item in list)
            {
                if (item.CallbackRetryTimes >= Constants.MaxCallbackRetryTimes)
                {
                    _logger.LogInformation("callback too many times and will be abandon,logId {logId}", item.LogId);
                }
                else
                {
                    item.CallbackRetryTimes++;

                    writer.WriteLine(JsonSerializer.Serialize(item, new JsonSerializerOptions { Converters = { new DateTimeConverter("yyyy-MM-dd HH:mm:ss") } }));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveCallbackParams error.");
        }
    }

    public void Dispose()
    {
        _cancellation?.Cancel();

        _runTask?.GetAwaiter().GetResult();
    }
}
