using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using XxlJob.Core.Internal;

namespace XxlJob.Core.Model;

[DataContract(Name = Constants.HandleCallbackParamJavaFullName)]
public class HandleCallbackParam
{
    public HandleCallbackParam(TriggerParam triggerParam, ReturnT result)
    {
        LogId = triggerParam.LogId;
        LogDateTime = triggerParam.LogDateTime;
        ExecuteResult = result;
    }

    public int CallbackRetryTimes { get; set; }

    [JsonPropertyName("logId")]
    public long LogId { get; set; }

    [JsonPropertyName("logDateTim")]
    public long LogDateTime { get; set; }

    /// <summary>
    /// 2.3.0以前版本
    /// </summary>
    [JsonPropertyName("executeResult")]
    public ReturnT? ExecuteResult { get; set; }

    /// <summary>
    /// 2.3.0版本使用的参数
    /// </summary>
    [JsonPropertyName("handleCode")]
    public int HandleCode => ExecuteResult?.Code ?? 500;

    /// <summary>
    /// 2.3.0版本使用的参数
    /// </summary>
    [JsonPropertyName("handleMsg")]
    public string? HandleMsg => ExecuteResult?.Msg;
}
