using System.Text.Json.Serialization;

namespace XxlJob.Core.Model;

public class IdleBeatRequest
{
    [JsonPropertyName("jobId")]
    public int JobId { get; set; }
}