using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.ReturnTJavaFullName)]
public class ReturnT
{
    public const int SuccessCode = 200;
    public const int FailCode = 500;

    public static ReturnT SUCCESS { get; } = new(SuccessCode, null);
    public static ReturnT FAIL { get; } = new(FailCode, null);

    // ReSharper disable once UnusedMember.Global
    public ReturnT() { }

    public ReturnT(int code, string? msg)
    {
        Code = code;
        Msg = msg;
    }


    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("content")]
    public object? Content { get; set; }

    public static ReturnT Failed(string msg) => new (FailCode, msg);

    public static ReturnT Success(string? msg) => new (SuccessCode, msg);
}
