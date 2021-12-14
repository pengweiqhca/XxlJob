using XxlJob.Core.Model;

namespace XxlJob.Core.Logger;

public interface IJobLogger
{
    void SetLogFile(long logTime, long logId);

    void Log(string pattern, params object?[] format);

    void LogError(Exception ex);

    LogResult ReadLog(long logTime, long logId, int fromLine);

    void LogSpecialFile(long logTime, long logId, string pattern, params object?[] format);
}
