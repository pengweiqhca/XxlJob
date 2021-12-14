using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.JavaClassFulName)]
public class JavaClass
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
