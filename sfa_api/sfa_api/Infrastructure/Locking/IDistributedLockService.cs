namespace sfa_api.Infrastructure.Locking;

public interface IDistributedLockService
{
    Task<IAsyncDisposable?> AcquireAsync(string resource, CancellationToken ct = default);
}
