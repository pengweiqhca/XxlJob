using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using XxlJob.Core.Internal;
using XxlJob.Core.Logger;
using XxlJob.Core.Model;
using XxlJob.Core.Queue;
using XxlJob.Core.TaskExecutors;

namespace XxlJob.Core;

/// <summary>
/// 负责实际的JOB轮询
/// </summary>
public class JobDispatcher
{
    private readonly TaskExecutorFactory _executorFactory;
    private readonly CallbackTaskQueue _callbackTaskQueue;
    private readonly IJobLogger _jobLogger;

    private readonly ConcurrentDictionary<int, JobTaskQueue> _runningQueue = new();


    private readonly ILogger<JobTaskQueue> _jobQueueLogger;
    private readonly ILogger<JobDispatcher> _logger;

    public JobDispatcher(TaskExecutorFactory executorFactory,
        CallbackTaskQueue callbackTaskQueue,
        IJobLogger jobLogger,
        ILoggerFactory loggerFactory)
    {
        _executorFactory = executorFactory;
        _callbackTaskQueue = callbackTaskQueue;
        _jobLogger = jobLogger;


        _jobQueueLogger = loggerFactory.CreateLogger<JobTaskQueue>();
        _logger = loggerFactory.CreateLogger<JobDispatcher>();
    }

    /// <summary>
    /// 尝试移除JobTask
    /// </summary>
    /// <param name="jobId"></param>
    /// <returns></returns>
    public bool TryRemoveJobTask(int jobId)
    {
        if (_runningQueue.TryGetValue(jobId, out var jobQueue))
        {
            jobQueue.Stop();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 执行队列，并快速返回结果
    /// </summary>
    /// <param name="triggerParam"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ReturnT Execute(TriggerParam triggerParam)
    {
        var executor = _executorFactory.GetTaskExecutor(triggerParam.GlueType);
        if (executor == null)
        {
            return ReturnT.Failed($"glueType[{triggerParam.GlueType}] is not supported ");
        }

        // 1. 根据JobId 获取 TaskQueue; 用于判断是否有正在执行的任务
        if (_runningQueue.TryGetValue(triggerParam.JobId, out var taskQueue))
        {
            if (taskQueue.Executor != executor) //任务执行器变更
            {
                return ChangeJobQueue(triggerParam, executor);
            }
        }

        if (taskQueue != null) //旧任务还在执行，判断执行策略
        {
            //丢弃后续的
            if (Constants.ExecutorBlockStrategy.DiscardLater == triggerParam.ExecutorBlockStrategy)
            {
                //存在还没执行完成的任务
                if (taskQueue.IsRunning())
                {
                    return ReturnT.Failed($"block strategy effect：{triggerParam.ExecutorBlockStrategy}");
                }
                //否则还是继续做
            }

            //覆盖较早的
            if (Constants.ExecutorBlockStrategy.CoverEarly == triggerParam.ExecutorBlockStrategy)
            {
                return taskQueue.Replace(triggerParam);
            }
        }

        return PushJobQueue(triggerParam, executor);
    }

    /// <summary>
    /// IdleBeat
    /// </summary>
    /// <param name="jobId"></param>
    /// <returns></returns>
    public ReturnT IdleBeat(int jobId) => _runningQueue.ContainsKey(jobId)
        ? new ReturnT(ReturnT.FailCode, "job thread is running or has trigger queue.")
        : ReturnT.SUCCESS;

    private void TriggerCallback(object sender, HandleCallbackParam callbackParam) =>
        _callbackTaskQueue.Push(callbackParam);

    private ReturnT PushJobQueue(TriggerParam triggerParam, ITaskExecutor executor)
    {
        if (_runningQueue.TryGetValue(triggerParam.JobId, out var jobQueue))
        {
            return jobQueue.Push(triggerParam);
        }

        //NewJobId
        jobQueue = new JobTaskQueue(executor, _jobLogger, _jobQueueLogger);

        jobQueue.CallBack += TriggerCallback;

        return _runningQueue.TryAdd(triggerParam.JobId, jobQueue) ? jobQueue.Push(triggerParam) : ReturnT.Failed("add running queue executor error");
    }

    private ReturnT ChangeJobQueue(TriggerParam triggerParam, ITaskExecutor executor)
    {
        if (_runningQueue.TryRemove(triggerParam.JobId, out var oldJobTask))
        {
            oldJobTask.CallBack -= TriggerCallback;
            oldJobTask.Dispose(); //释放原来的资源
        }

        var jobQueue = new JobTaskQueue(executor, _jobLogger, _jobQueueLogger);

        jobQueue.CallBack += TriggerCallback;

        return _runningQueue.TryAdd(triggerParam.JobId, jobQueue) ? jobQueue.Push(triggerParam) : ReturnT.Failed(" replace running queue executor error");
    }
}
