using System.Text.Json.Serialization;

namespace XxlJob.Core.Model;

public class KillRequest
{
    [JsonPropertyName("jobId")]
    public int JobId { get; set; }
}
