using sfa_api.Infrastructure.Locking;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only IDistributedLockService that always grants the lock immediately.
/// Replaces PostgresAdvisoryLockService in integration tests, since pg_try_advisory_lock
/// is a PostgreSQL-only function that cannot run against SQLite in-memory.
/// </summary>
public sealed class NoOpDistributedLockService : IDistributedLockService
{
    public Task<IAsyncDisposable?> AcquireAsync(string resource, CancellationToken ct = default)
        => Task.FromResult<IAsyncDisposable?>(NoOpLock.Instance);

    private sealed class NoOpLock : IAsyncDisposable
    {
        public static readonly NoOpLock Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
