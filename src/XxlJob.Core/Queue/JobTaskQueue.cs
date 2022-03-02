using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using XxlJob.Core.Logger;
using XxlJob.Core.Model;
using XxlJob.Core.TaskExecutors;

namespace XxlJob.Core.Queue;

public class JobTaskQueue : IDisposable
{
    private readonly IJobLogger _jobLogger;
    private readonly ILogger<JobTaskQueue> _logger;
    private readonly ConcurrentQueue<(TriggerParam, ActivityContext)> _taskQueue = new();
    private readonly ConcurrentDictionary<long, byte> _idInQueue = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public JobTaskQueue(ITaskExecutor executor, IJobLogger jobLogger, ILogger<JobTaskQueue> logger)
    {
        Executor = executor;
        _jobLogger = jobLogger;
        _logger = logger;
    }

    public ITaskExecutor Executor { get; }

    public event EventHandler<HandleCallbackParam>? CallBack;

    public bool IsRunning() => _cancellationTokenSource != null;

    /// <summary>
    /// 覆盖之前的队列
    /// </summary>
    /// <param name="triggerParam"></param>
    /// <returns></returns>
    public ReturnT Replace(TriggerParam triggerParam)
    {
        Stop();

        while (!_taskQueue.IsEmpty)
        {
            _taskQueue.TryDequeue(out _);
        }

        _idInQueue.Clear();

        return Push(triggerParam);
    }

    public ReturnT Push(TriggerParam triggerParam)
    {
        if (!_idInQueue.TryAdd(triggerParam.LogId, 0))
        {
            _logger.LogWarning("repeat job task,logId={logId},jobId={jobId}", triggerParam.LogId, triggerParam.JobId);
            return ReturnT.Failed("repeat job task!");
        }

        //this._logger.LogWarning("add job with logId={logId},jobId={jobId}",triggerParam.LogId,triggerParam.JobId);

        _taskQueue.Enqueue((triggerParam, Activity.Current?.Context ?? default));
        StartTask();
        return ReturnT.SUCCESS;
    }

    public void Stop() => _cancellationTokenSource?.Cancel();

    public void Dispose()
    {
        Stop();

        while (!_taskQueue.IsEmpty)
        {
            _taskQueue.TryDequeue(out _);
        }

        _idInQueue.Clear();
    }

    private void StartTask()
    {
        if (_cancellationTokenSource != null) return; //running

        _cancellationTokenSource = new CancellationTokenSource();

        using var __ = ExecutionContext.SuppressFlow();

        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_taskQueue.IsEmpty) break;

                ReturnT? result = null;
                TriggerParam? triggerParam = null;
                Activity? activity = null;
                try
                {
                    if (_taskQueue.TryDequeue(out var task))
                    {
                        ActivityContext activityContext;

                        (triggerParam, activityContext) = task;

                        if (!_idInQueue.TryRemove(triggerParam.LogId, out _))
                        {
                            _logger.LogWarning("remove queue failed,logId={logId},jobId={jobId},exists={exists}"
                                , triggerParam.LogId, triggerParam.JobId, _idInQueue.ContainsKey(triggerParam.LogId));
                        }

                        if (string.IsNullOrWhiteSpace(triggerParam.ExecutorHandler))
                        {
                            _jobLogger.Log("<br>----------- job handler of job {0} is null or empty.", triggerParam.JobId);

                            continue;
                        }

                        //set log file;
                        _jobLogger.SetLogFile(triggerParam.LogDateTime, triggerParam.LogId);

                        activity = ActivityHelper.XxlJobSource.StartActivity(triggerParam.ExecutorHandler!, ActivityKind.Consumer, activityContext);

                        if (activity != null)
                            _jobLogger.Log("<br>----------- xxl-job job execute start -----------<br>----------- ActivityId:{0}<br>----------- Param:{1}", activity.Id, triggerParam.ExecutorParams);
                        else
                            _jobLogger.Log("<br>----------- xxl-job job execute start -----------<br>----------- Param:{0}", triggerParam.ExecutorParams);

                        result = await ExecuteTask(triggerParam, _cancellationTokenSource.Token).ConfigureAwait(false);

                        _jobLogger.Log("<br>----------- xxl-job job execute end(finish) -----------<br>----------- ReturnT:" + result.Code);

                        if (activity != null)
                        {
                            if (result.Code == ReturnT.SuccessCode) activity.SetStatus(ActivityStatusCode.Ok);
                            else activity.SetStatus(ActivityStatusCode.Error, result.Msg);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Dequeue Task Failed");
                    }
                }
                catch (OperationCanceledException ex)
                {
                    result = ReturnT.Failed("Task is cancelled: " + ex.Message);

                    _jobLogger.Log("<br>----------- JobThread Exception:" + ex.Message + "<br>----------- xxl-job job execute end(cancelled) -----------");

                    activity?.RecordException(ex);
                }
                catch (Exception ex)
                {
                    result = ReturnT.Failed("Execute Task Failed: " + ex.Message);

                    _jobLogger.Log("<br>----------- JobThread Exception:" + ex.Message + "<br>----------- xxl-job job execute end(error) -----------");

                    activity?.RecordException(ex);
                }
                finally
                {
                    activity?.Dispose();
                }

                if (triggerParam != null)
                {
                    CallBack?.Invoke(this, new HandleCallbackParam(triggerParam, result ?? ReturnT.FAIL));
                }
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

        }, _cancellationTokenSource.Token);
    }

    private async Task<ReturnT> ExecuteTask(TriggerParam triggerParam, CancellationToken cancellationToken)
    {
        var task = Executor.Execute(triggerParam, cancellationToken);

        if (task.IsCompleted) return await task.ConfigureAwait(false);

        var tcs = new TaskCompletionSource<object?>();
#if NETFRAMEWORK
        using (cancellationToken.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), tcs))
#else
        await using (cancellationToken.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), tcs).ConfigureAwait(false))
#endif
            if (await Task.WhenAny(task, tcs.Task).ConfigureAwait(false) != task)
                throw new OperationCanceledException(cancellationToken);

        return await task.ConfigureAwait(false);
    }
}
