using System.Net;
using XxlJob.Core.Internal;
using XxlJob.Core.Model;

namespace XxlJob.Core.DefaultHandlers;

[JobHandler("simpleHttpJobHandler")]
public class SimpleHttpJobHandler : IJobHandler
{
    private readonly IHttpClientFactory _httpClientFactory;

    public SimpleHttpJobHandler(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<ReturnT> Execute(JobExecuteContext context)
    {
        if (string.IsNullOrEmpty(context.JobParameter))
        {
            return ReturnT.Failed("url is empty");
        }

        var url = context.JobParameter;

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return ReturnT.Failed("url format is not valid");
        }

        context.JobLogger.Log("Get Request Data:{0}", context.JobParameter);

        using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClientName);
        try
        {
            var response = await client.GetAsync(url).ConfigureAwait(false);
            if (response == null)
            {
                context.JobLogger.Log("call remote error,response is null");
                return ReturnT.Failed("call remote error,response is null");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                context.JobLogger.Log("call remote error,response statusCode ={0}", response.StatusCode);
                return ReturnT.Failed("call remote error,response statusCode =" + response.StatusCode);
            }

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            context.JobLogger.Log("<br/> call remote success ,response is : <br/> {0}", body);
            return ReturnT.SUCCESS;
        }
        catch (Exception ex)
        {
            context.JobLogger.LogError(ex);
            return ReturnT.Failed(ex.Message);
        }
    }
}
