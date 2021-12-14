using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.RpcRequestJavaFullName)]
public class RpcRequest
{
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    //[JsonPropertyName("serverAddress")]
    //public string ServerAddress{ get; set; }

    [JsonPropertyName("createMillisTime" )]
    public long CreateMillisTime{ get; set; }

    [JsonPropertyName("accessToken" )]
    public string? AccessToken { get; set; }

    [JsonPropertyName("className" )]
    public string? ClassName { get; set; }

    [JsonPropertyName("methodName" )]
    public string? MethodName { get; set; }

    [JsonPropertyName("version" )]
    public string? Version { get; set; }

    [JsonPropertyName("parameterTypes")]
    public IList<object>? ParameterTypes { get; set; }

    [JsonPropertyName("parameters")]
    public IList<object>? Parameters { get; set; }
}
