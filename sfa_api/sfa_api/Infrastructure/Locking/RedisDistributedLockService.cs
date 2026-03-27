using System.Net.Sockets;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using sfa_api.Common.Errors;

namespace sfa_api.Infrastructure.Locking;

public class RedisDistributedLockService(
    IDistributedLockFactory factory,
    ILogger<RedisDistributedLockService> logger) : IDistributedLockService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);

    public async Task<IAsyncDisposable?> AcquireAsync(string resource, CancellationToken ct = default)
    {
        IRedLock redLock;
        try
        {
            redLock = await factory.CreateLockAsync(
                resource: resource,
                expiryTime: LockExpiry,
                waitTime: TimeSpan.Zero,
                retryTime: TimeSpan.Zero,
                cancellationToken: ct);
        }
        catch (RedisConnectionException ex)
        {
            logger.LogError(ex, "Redis connection failed while acquiring lock for resource {Resource}", resource);
            throw new LockServiceUnavailableException();
        }
        catch (SocketException ex)
        {
            logger.LogError(ex, "Socket error while acquiring lock for resource {Resource}", resource);
            throw new LockServiceUnavailableException();
        }

        if (!redLock.IsAcquired)
        {
            logger.LogWarning("Failed to acquire Redis lock for resource {Resource}. Status: {Status}",
                resource, redLock.Status);
            redLock.Dispose();
            return null;
        }

        logger.LogDebug("Acquired Redis lock for resource {Resource}", resource);
        return new RedLockHandle(redLock, resource, logger);
    }

    private sealed class RedLockHandle(IRedLock redLock, string resource, ILogger logger) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            logger.LogDebug("Released Redis lock for resource {Resource}", resource);
            redLock.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    // ── Factory builder ────────────────────────────────────────────────────

    public static IDistributedLockFactory CreateFactory(IConfiguration config)
    {
        var restUrl = config["UPSTASH_REDIS_REST_URL"]
            ?? throw new InvalidOperationException("UPSTASH_REDIS_REST_URL is not configured.");
        var token = config["UPSTASH_REDIS_REST_TOKEN"]
            ?? throw new InvalidOperationException("UPSTASH_REDIS_REST_TOKEN is not configured.");

        // Extract hostname from REST URL — StackExchange.Redis connects via TCP, not HTTP
        var host = new Uri(restUrl).Host;
        var connectionString = $"{host}:6379,password={token},ssl=true,abortConnect=false";

        var multiplexer = ConnectionMultiplexer.Connect(connectionString);
        return RedLockFactory.Create([new RedLockMultiplexer(multiplexer)]);
    }
}
