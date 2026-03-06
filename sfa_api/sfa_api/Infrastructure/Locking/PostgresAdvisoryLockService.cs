using Npgsql;

namespace sfa_api.Infrastructure.Locking;

public class PostgresAdvisoryLockService(IConfiguration config,
    ILogger<PostgresAdvisoryLockService> logger) : IDistributedLockService
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;
    private readonly ILogger<PostgresAdvisoryLockService> _logger = logger;

    public async Task<IAsyncDisposable?> AcquireAsync(
        string resource, CancellationToken ct = default)
    {
        var lockKey = (long)resource.GetHashCode();
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT pg_try_advisory_lock(@key)";
        cmd.Parameters.AddWithValue("key", lockKey);

        var acquired = (bool)(await cmd.ExecuteScalarAsync(ct))!;

        if (!acquired)
        {
            _logger.LogWarning(
                "Failed to acquire advisory lock for resource {Resource}", resource);
            await conn.DisposeAsync();
            return null;
        }

        _logger.LogDebug("Acquired advisory lock for {Resource}", resource);
        return new AdvisoryLockHandle(conn, lockKey, resource, _logger);
    }

    private sealed class AdvisoryLockHandle(NpgsqlConnection conn, long lockKey,
        string resource, ILogger logger) : IAsyncDisposable
    {
        private readonly NpgsqlConnection _conn = conn;
        private readonly long _lockKey = lockKey;
        private readonly string _resource = resource;
        private readonly ILogger _logger = logger;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cmd = _conn.CreateCommand();
                cmd.CommandText = "SELECT pg_advisory_unlock(@key)";
                cmd.Parameters.AddWithValue("key", _lockKey);
                await cmd.ExecuteScalarAsync();
                _logger.LogDebug("Released advisory lock for {Resource}", _resource);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release advisory lock for {Resource}", _resource);
            }
            finally
            {
                await _conn.DisposeAsync();
            }
        }
    }
}
