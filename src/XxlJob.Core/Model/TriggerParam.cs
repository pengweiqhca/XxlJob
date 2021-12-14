using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace XxlJob.Core.Model;

[DataContract]
public class TriggerParam
{
    [JsonPropertyName("jobId")]
    public int JobId { get; set; }

    [JsonPropertyName("executorHandler")]
    public string? ExecutorHandler { get; set; }

    [JsonPropertyName("executorParams")]
    public string? ExecutorParams { get; set; }

    [JsonPropertyName("executorBlockStrategy")]
    public string? ExecutorBlockStrategy { get; set; }

    [JsonPropertyName("executorTimeout")]
    public int ExecutorTimeout{ get; set; }

    [JsonPropertyName("logId")]
    public long LogId { get; set; }

    [JsonPropertyName("logDateTime")]
    public long LogDateTime{ get; set; }

    [JsonPropertyName("glueType")]
    public string? GlueType { get; set; }

    [JsonPropertyName("glueSource")]
    public string? GlueSource { get; set; }

    [JsonPropertyName("glueUpdatetime")]
    public long GlueUpdateTime{ get; set; }

    [JsonPropertyName("broadcastIndex")]
    public int BroadcastIndex{ get; set; }

    [JsonPropertyName("broadcastTotal")]
    public int BroadcastTotal{ get; set; }
}
