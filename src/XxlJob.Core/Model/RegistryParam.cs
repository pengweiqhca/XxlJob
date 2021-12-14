using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.RegistryParamJavaFullName)]
public class RegistryParam
{
    [JsonPropertyName("registryGroup")]
    public string? RegistryGroup { get; set; }

    [JsonPropertyName("registryKey")]
    public string? RegistryKey { get; set; }


    [JsonPropertyName("registryValue")]
    public string? RegistryValue { get; set; }
}
