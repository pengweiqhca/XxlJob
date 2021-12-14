using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using XxlJob.Core.Config;
using XxlJob.Core.Internal;
using XxlJob.Core.Model;

namespace XxlJob.Core.Logger;

public class JobLogger : IJobLogger
{
    private readonly ILogger<JobLogger> _logger;

    private readonly AsyncLocal<string> _logFileName = new();

    private readonly XxlJobExecutorOptions _options;
    public JobLogger(IOptions<XxlJobExecutorOptions> optionsAccessor, ILogger<JobLogger> logger)
    {
        _logger = logger;
        _options = optionsAccessor.Value;
    }

    public void SetLogFile(long logTime, long logId)
    {
        try
        {
            var filePath = MakeLogFileName(logTime, logId);
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                CleanOldLogs();
            }
            _logFileName.Value = filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetLogFileName error.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Log(string pattern, params object?[] format)
    {
        var appendLog = format.Length == 0 ? pattern : string.Format(pattern, format);

        LogDetail(GetLogFileName(), (EnhancedStackFrame)EnhancedStackTrace.Current().GetFrame(0), appendLog);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogError(Exception ex)
    {
        LogDetail(GetLogFileName(), (EnhancedStackFrame)EnhancedStackTrace.Current().GetFrame(0), ex.Message + ex.StackTrace);
    }

    public LogResult ReadLog(long logTime, long logId, int fromLine)
    {
        var filePath = MakeLogFileName(logTime, logId);
        if (string.IsNullOrEmpty(filePath))
        {
            return new LogResult(fromLine, 0, "readLog fail, logFile not found", true);
        }
        if (!File.Exists(filePath))
        {
            return new LogResult(fromLine, 0, "readLog fail, logFile not exists", true);
        }

        // read file
        var logContentBuffer = new StringBuilder();
        var toLineNum = 0;
        try
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                toLineNum++;
                if (toLineNum >= fromLine)
                {
                    logContentBuffer.AppendLine(line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadLog error.");
        }

        // result
        var logResult = new LogResult(fromLine, toLineNum, logContentBuffer.ToString(), false);
        return logResult;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogSpecialFile(long logTime, long logId, string pattern, params object?[] format)
    {
        LogDetail(MakeLogFileName(logTime, logId), (EnhancedStackFrame)EnhancedStackTrace.Current().GetFrame(0), string.Format(pattern, format));
    }

    private string GetLogFileName()
    {
        return _logFileName.Value;
    }

    private string MakeLogFileName(long logDateTime, long logId)
    {
        //log fileName like: logPath/HandlerLogs/yyyy-MM-dd/9999.log
        return Path.Combine(_options.LogPath, Constants.HandleLogsDirectory,
            DateTimeOffset.FromUnixTimeMilliseconds(logDateTime).ToString("yyyy-MM-dd"), $"{logId}.log");
    }

    private void LogDetail(string logFileName, EnhancedStackFrame callInfo, string appendLog)
    {
        if (string.IsNullOrEmpty(logFileName)) return;

        var formatAppendLog = $"{DateTime.Now:s} [{callInfo.MethodInfo.DeclaringType?.FullName}#{callInfo.MethodInfo.Name}]-[line {callInfo.GetFileLineNumber()}]-[thread {Thread.CurrentThread.ManagedThreadId}] {appendLog}{Environment.NewLine}";

        try
        {
            File.AppendAllText(logFileName, formatAppendLog, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogDetail error");
        }
    }

    private void CleanOldLogs()
    {
        if (_options.LogRetentionDays <= 0)
        {
            _options.LogRetentionDays = Constants.DefaultLogRetentionDays;
        }

        Task.Run(() =>
        {
            try
            {
                var handlerLogsDir = new DirectoryInfo(Path.Combine(_options.LogPath, Constants.HandleLogsDirectory));
                if (!handlerLogsDir.Exists)
                {
                    return;
                }

                var today = DateTime.UtcNow.Date;
                foreach (var dir in handlerLogsDir.GetDirectories())
                {
                    if (DateTime.TryParse(dir.Name, out var dirDate))
                    {
                        if (today.Subtract(dirDate.Date).Days > _options.LogRetentionDays)
                        {
                            dir.Delete(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CleanOldLogs error.");
            }
        });
    }
}
