using System.Diagnostics;
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
    private readonly XxlJobOptions _options;

    public XxlRestfulServiceHandler(IOptions<XxlJobOptions> optionsAccessor,
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
            throw new ArgumentNullException(nameof(XxlJobOptions));
        }
    }

    public bool SupportedMethod(string method)
    {
        if (string.IsNullOrEmpty(method)) return false;

        return method.ToLower() is "beat" or "idleBeat" or "run" or "kill" or "log";
    }

    public async Task HandlerAsync(IXxlJobContext context, CancellationToken cancellationToken)
    {
        ReturnT? ret = null;

        if (!string.IsNullOrEmpty(_options.AccessToken) &&
            context.TryGetHeader("XXL-JOB-ACCESS-TOKEN", out var tokenValues) &&
            _options.AccessToken != tokenValues.FirstOrDefault())
        {
            ret = ReturnT.Failed("ACCESS-TOKEN Auth Fail");

            await context.WriteResponse(ret, cancellationToken).ConfigureAwait(false);

            return;
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
            _logger.LogError(ex, "Handle command fail. " + ex.Message);

            ret = ReturnT.Failed("Executor internal error.");

            ret.Content = ex.ToStringDemystified();
        }

        if (!cancellationToken.IsCancellationRequested)
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
    /// ??????
    /// </summary>
    /// <param name="triggerParam"></param>
    /// <returns></returns>
    private ReturnT Run(TriggerParam? triggerParam) =>
        triggerParam == null ? ReturnT.Failed("Run Error") : _jobDispatcher.Execute(triggerParam);

    #endregion
}
