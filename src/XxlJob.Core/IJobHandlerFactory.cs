namespace XxlJob.Core;

public interface IJobHandlerFactory
{
    IJobHandler? GetJobHandler(IServiceProvider provider, string handlerName);
}