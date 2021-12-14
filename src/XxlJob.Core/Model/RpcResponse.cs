using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.RpcResponseJavaFullName)]
public class RpcResponse
{
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("errorMsg")]
    public string? ErrorMsg { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    public bool IsError => ErrorMsg != null;
}
