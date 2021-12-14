using System.Text.Json.Serialization;

namespace XxlJob.Core.Model;

public class LogRequest
{
    [JsonPropertyName("logDateTim")]
    public long LogDateTime { get; set; }

    [JsonPropertyName("logId")]
    public int LogId { get; set; }

    [JsonPropertyName("fromLineNum")]
    public int FromLineNum { get; set; }

}
