namespace XxlJob.Core;

public interface IExecutorRegistry
{
    void BeginRegistry();

    Task RegistryAsync(CancellationToken cancellationToken);
}
