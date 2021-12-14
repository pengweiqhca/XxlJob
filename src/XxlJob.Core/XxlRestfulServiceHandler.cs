using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XxlJob.Core.Config;
using XxlJob.Core.Logger;
using XxlJob.Core.Model;

namespace XxlJob.Core;

public class XxlRestfulServiceHandler
{
    private readonly JobDispatcher _jobDispatcher;
    private readonly IJobLogger _jobLogger;
    private readonly ILogger<XxlRestfulServiceHandler> _logger;
    private readonly XxlJobExecutorOptions _options;

    public XxlRestfulServiceHandler(IOptions<XxlJobExecutorOptions> optionsAccessor,
        JobDispatcher jobDispatcher,
        IJobLogger jobLogger,
        ILogger<XxlRestfulServiceHandler> logger)
    {

        _jobDispatcher = jobDispatcher;
        _jobLogger = jobLogger;
        _logger = logger;

        _options = optionsAccessor.Value;
        if (_options == null)
        {
            throw new ArgumentNullException(nameof(XxlJobExecutorOptions));
        }
    }

    public bool SupportedMethod(string method)
    {
        if (string.IsNullOrEmpty(method)) return false;

        method = method.ToLower();

        return method == "beat" || method == "idleBeat" || method == "run" || method == "kill" || method == "log";
    }

    public async Task HandlerAsync(IXxlJobContext context, CancellationToken cancellationToken)
    {
        ReturnT? ret = null;

        if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            var reqToken = "";
            if (context.TryGetHeader("XXL-JOB-ACCESS-TOKEN", out var tokenValues))
            {
                reqToken = tokenValues[0];
            }
            if (_options.AccessToken != reqToken)
            {
                ret = ReturnT.Failed("ACCESS-TOKEN Auth Fail");

                await context.WriteResponse(ret, cancellationToken).ConfigureAwait(false);

                return;
            }
        }

        try
        {
            ret = context.Method switch
            {
                "beat" => Beat(),
                "idleBeat" => IdleBeat(await context.ReadRequest<IdleBeatRequest>(cancellationToken).ConfigureAwait(false)),
                "run" => Run(await context.ReadRequest<TriggerParam>(cancellationToken).ConfigureAwait(false)),
                "kill" => Kill(await context.ReadRequest<KillRequest>(cancellationToken).ConfigureAwait(false)),
                "log" => Log(await context.ReadRequest<LogRequest>(cancellationToken).ConfigureAwait(false)),
                _ => ret
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "响应出错" + ex.Message);

            ret = ReturnT.Failed("执行器内部错误");
        }

        await context.WriteResponse(ret ?? ReturnT.Failed($"method {context.Method}  is not impl"), cancellationToken).ConfigureAwait(false);
    }

    #region rpc service

    private static ReturnT Beat() => ReturnT.SUCCESS;

    private ReturnT IdleBeat(IdleBeatRequest? req) =>
        req == null ? ReturnT.Failed("IdleBeat Error") : _jobDispatcher.IdleBeat(req.JobId);

    private ReturnT Kill(KillRequest? req) =>
        req == null ? ReturnT.Failed("Kill Error") : _jobDispatcher.TryRemoveJobTask(req.JobId) ? ReturnT.SUCCESS : ReturnT.Success("job thread already killed.");

    /// <summary>
    ///  read Log
    /// </summary>
    /// <returns></returns>
    private ReturnT Log(LogRequest? req)
    {
        if (req == null)
        {
            return ReturnT.Failed("Log Error");
        }

        var ret = ReturnT.Success(null);

        ret.Content = _jobLogger.ReadLog(req.LogDateTime, req.LogId, req.FromLineNum);

        return ret;
    }

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="triggerParam"></param>
    /// <returns></returns>
    private ReturnT Run(TriggerParam? triggerParam) =>
        triggerParam == null ? ReturnT.Failed("Run Error") : _jobDispatcher.Execute(triggerParam);

    #endregion
}
