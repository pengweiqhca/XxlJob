using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
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

        if (_options == null) throw new ArgumentNullException(nameof(XxlJobOptions));
    }

    public virtual bool IsSupportedRequest(IXxlJobContext context)
    {
        if (!"POST".Equals(context.HttpMethod, StringComparison.OrdinalIgnoreCase)) return false;

        if (!context.TryGetHeader("Content-Type", out var values)) return false;

        var contentType = values.FirstOrDefault();

        if (string.IsNullOrEmpty(contentType) ||
            !contentType.ToLower().StartsWith("application/json", StringComparison.Ordinal)) return false;

        if (string.IsNullOrEmpty(context.Action)) return false;

        return context.Action is "beat" or "idleBeat" or "run" or "kill" or "log";
    }

    protected virtual HttpStatusCode CheckRequest(IXxlJobContext context, out string? message)
    {
        message = null;

        if (!"POST".Equals(context.HttpMethod, StringComparison.OrdinalIgnoreCase))
        {
            return HttpStatusCode.MethodNotAllowed;
        }

        if (!context.TryGetHeader("Content-Type", out var values))
        {
            message = "Missing 'Content-Type' header.";

            return HttpStatusCode.BadRequest;
        }

        var contentType = values.FirstOrDefault();
        if (string.IsNullOrEmpty(contentType))
        {
            message = "Invalid 'Content-Type' header.";

            return HttpStatusCode.BadRequest;
        }

        if (!contentType.ToLower().StartsWith("application/json", StringComparison.Ordinal))
        {
            message = "Invalid 'Content-Type' header value (must start with 'application/json').";

            return HttpStatusCode.UnsupportedMediaType;
        }

        if (string.IsNullOrEmpty(context.Action))
        {
            message = "Missing 'action'";

            return HttpStatusCode.NotFound;
        }

        if (context.Action is "beat" or "idleBeat" or "run" or "kill" or "log") return HttpStatusCode.OK;

        message = $"Unsupported action {context.Action}";

        return HttpStatusCode.NotFound;
    }

    public async Task HandlerAsync(IXxlJobContext context, CancellationToken cancellationToken)
    {
        ReturnT? ret = null;

        if (CheckRequest(context, out var message) is var statusCode && statusCode != HttpStatusCode.OK)
        {
            ret = new ReturnT((int)statusCode, message);

            await context.WriteResponse(statusCode, ret, cancellationToken).ConfigureAwait(false);

            return;
        }

        if (!string.IsNullOrEmpty(_options.AccessToken) &&
            context.TryGetHeader("XXL-JOB-ACCESS-TOKEN", out var tokenValues) &&
            _options.AccessToken != tokenValues.FirstOrDefault())
        {
            ret = ReturnT.Failed("ACCESS-TOKEN Auth Fail");

            await context.WriteResponse(HttpStatusCode.OK, ret, cancellationToken).ConfigureAwait(false);

            return;
        }

        try
        {
            ret = context.Action switch
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
            await context.WriteResponse(HttpStatusCode.OK, ret ?? ReturnT.Failed($"action {context.Action}  is not impl"), cancellationToken).ConfigureAwait(false);
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
