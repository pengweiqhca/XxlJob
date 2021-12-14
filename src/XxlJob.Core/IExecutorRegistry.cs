namespace XxlJob.Core;

public interface IExecutorRegistry
{
    Task RegistryAsync(CancellationToken cancellationToken);
}
