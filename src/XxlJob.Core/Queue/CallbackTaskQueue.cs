using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using XxlJob.Core.Config;
using XxlJob.Core.Logger;
using XxlJob.Core.Model;

namespace XxlJob.Core.Queue;

public class CallbackTaskQueue:IDisposable
{
    private readonly AdminClient _adminClient;
    private readonly IJobLogger _jobLogger;
    private readonly RetryCallbackTaskQueue _retryQueue;
    private readonly ILogger<CallbackTaskQueue> _logger;
    private readonly ConcurrentQueue<HandleCallbackParam> _taskQueue = new();

    private bool _stop;

    private bool _isRunning;

    private readonly int _callbackInterval;

    private Task? _runTask;

    public CallbackTaskQueue(AdminClient adminClient,IJobLogger jobLogger,IOptions<XxlJobOptions> optionsAccessor
        , ILoggerFactory loggerFactory)
    {
        _adminClient = adminClient;
        _jobLogger = jobLogger;

        _callbackInterval = optionsAccessor.Value.CallBackInterval;

        _retryQueue = new RetryCallbackTaskQueue(optionsAccessor.Value.LogPath,
            Push,
            loggerFactory.CreateLogger<RetryCallbackTaskQueue>());

        _logger = loggerFactory.CreateLogger<CallbackTaskQueue>();
    }

    public void Push(HandleCallbackParam callbackParam)
    {
        _taskQueue.Enqueue(callbackParam);
        StartCallBack();
    }
    public void Dispose()
    {
        _stop = true;
        _retryQueue.Dispose();
        _runTask?.GetAwaiter().GetResult();
    }

    private void StartCallBack()
    {
        if ( _isRunning)
        {
            return;
        }

        _runTask = Task.Run(async () =>
        {
            _logger.LogDebug("start to callback");
            _isRunning = true;
            while (!_stop)
            {
                await DoCallBack().ConfigureAwait(false);
                if (_taskQueue.IsEmpty)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_callbackInterval)).ConfigureAwait(false);
                }
            }
            _logger.LogDebug("end to callback");
            _isRunning = false;
        });

    }

    private async Task DoCallBack()
    {
        var list = new List<HandleCallbackParam>();

        if(!_taskQueue.TryDequeue(out var item))
        {
            return;
        }

        list.Add(item);

        ReturnT result;
        try
        {
            result = await _adminClient.Callback(list).ConfigureAwait(false);
        }
        catch (Exception ex){
            _logger.LogError(ex,"trigger callback error:{error}",ex.Message);
            result = ReturnT.Failed(ex.Message);
            _retryQueue.Push(list);
        }

        LogCallBackResult(result, list);
    }

    private void LogCallBackResult(ReturnT result,List<HandleCallbackParam> list)
    {
        foreach (var param in list)
        {
            _jobLogger.LogSpecialFile(param.LogDateTime, param.LogId, result.Msg??"Success");
        }
    }
}
