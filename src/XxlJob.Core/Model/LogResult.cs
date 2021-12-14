using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.LogResultJavaFullName)]
public class LogResult
{
    public LogResult(int fromLine ,int toLine,string content,bool isEnd)
    {
        FromLineNum = fromLine;
        ToLineNum = toLine;
        LogContent = content;
        IsEnd = isEnd;
    }

    [JsonPropertyName("fromLineNum")]
    public int FromLineNum { get; set; }
    [JsonPropertyName("toLineNum")]
    public int ToLineNum { get; set; }
    [JsonPropertyName("logContent")]
    public string LogContent { get; set; }
    [JsonPropertyName("isEnd")]
    public bool IsEnd { get; set; }
}
