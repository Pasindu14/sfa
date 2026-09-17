using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using sfa_api.Common.Audit;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Infrastructure.Caching;

public sealed class PostgresTokenRevocationServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    public PostgresTokenRevocationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        // EnsureCreated() can't run (PostgreSQL sequences) — create just the table under test.
        _db.Database.ExecuteSqlRaw(
            "CREATE TABLE \"RevokedTokens\" (\"Jti\" TEXT NOT NULL PRIMARY KEY, \"RevokedAt\" TEXT NOT NULL, \"ExpiresAt\" TEXT NOT NULL)");
    }

    public void Dispose()
    {
        _db.Dispose();
        _memoryCache.Dispose();
    }

    private PostgresTokenRevocationService CreateSut(IDistributedCache distributed)
        => new(_db, distributed, _memoryCache, NullLogger<PostgresTokenRevocationService>.Instance);

    private static MemoryDistributedCache InProcessDistributedCache()
        => new(Options.Create(new MemoryDistributedCacheOptions()));

    // ── Shared cache (Redis) configured ────────────────────────────────────

    [Fact]
    public async Task IsRevoked_SharedCacheMiss_ReturnsFalseWithoutQueryingPostgres()
    {
        var distributed = new Mock<IDistributedCache>();
        distributed.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((byte[]?)null);
        var sut = CreateSut(distributed.Object);

        // A row exists in Postgres, but the shared cache is authoritative when configured: the
        // point of the change is that the per-request check never touches the database.
        _db.RevokedTokens.Add(new RevokedToken { Jti = "db-only", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        var revoked = await sut.IsRevokedAsync("db-only");

        revoked.Should().BeFalse();
        distributed.Verify(c => c.GetAsync(PostgresTokenRevocationService.DistributedKey("db-only"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsRevoked_SharedCacheHit_ReturnsTrue()
    {
        var distributed = new Mock<IDistributedCache>();
        distributed.Setup(c => c.GetAsync(PostgresTokenRevocationService.DistributedKey("jti-1"), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new byte[] { 1 });
        var sut = CreateSut(distributed.Object);

        (await sut.IsRevokedAsync("jti-1")).Should().BeTrue();
    }

    [Fact]
    public async Task Revoke_WritesPostgresAndSharedCacheWithRemainingLifetimeTtl()
    {
        var distributed = new Mock<IDistributedCache>();
        DistributedCacheEntryOptions? captured = null;
        distributed.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                   .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((_, _, o, _) => captured = o)
                   .Returns(Task.CompletedTask);
        var sut = CreateSut(distributed.Object);

        await sut.RevokeAsync("jti-2", DateTime.UtcNow.AddMinutes(30));

        (await _db.RevokedTokens.AsNoTracking().AnyAsync(x => x.Jti == "jti-2")).Should().BeTrue();
        distributed.Verify(c => c.SetAsync(PostgresTokenRevocationService.DistributedKey("jti-2"), It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        captured!.AbsoluteExpirationRelativeToNow.Should().BeCloseTo(TimeSpan.FromMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Revoke_SharedCacheWriteFails_TokenStillRejectedOnThisInstance()
    {
        var distributed = new Mock<IDistributedCache>();
        distributed.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("redis down"));
        distributed.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((byte[]?)null);
        var sut = CreateSut(distributed.Object);

        await sut.RevokeAsync("jti-3", DateTime.UtcNow.AddMinutes(30));

        (await sut.IsRevokedAsync("jti-3")).Should().BeTrue();
    }

    [Fact]
    public async Task IsRevoked_SharedCacheThrows_FallsBackToPostgres()
    {
        var distributed = new Mock<IDistributedCache>();
        distributed.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("redis down"));
        var sut = CreateSut(distributed.Object);

        _db.RevokedTokens.Add(new RevokedToken { Jti = "jti-4", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        (await sut.IsRevokedAsync("jti-4")).Should().BeTrue();
        (await sut.IsRevokedAsync("not-revoked")).Should().BeFalse();
    }

    // ── No shared cache (in-process MemoryDistributedCache fallback) ───────

    [Fact]
    public async Task IsRevoked_NoSharedCache_ReadsPostgresAndMemoisesNegative()
    {
        var sut = CreateSut(InProcessDistributedCache());

        (await sut.IsRevokedAsync("jti-5")).Should().BeFalse();

        // Another instance revokes it (row appears in Postgres only). Within the short memo window
        // this instance may still accept it — that is the documented bounded staleness.
        _db.RevokedTokens.Add(new RevokedToken { Jti = "jti-5", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        (await sut.IsRevokedAsync("jti-5")).Should().BeFalse();
    }

    [Fact]
    public async Task Revoke_AfterNegativeMemo_IsRejectedImmediatelyOnSameInstance()
    {
        var sut = CreateSut(InProcessDistributedCache());

        (await sut.IsRevokedAsync("jti-6")).Should().BeFalse();   // memoises "not revoked"

        await sut.RevokeAsync("jti-6", DateTime.UtcNow.AddMinutes(30));

        (await sut.IsRevokedAsync("jti-6")).Should().BeTrue();

        // A fresh scoped instance (next request) shares the process-local cache.
        (await CreateSut(InProcessDistributedCache()).IsRevokedAsync("jti-6")).Should().BeTrue();
    }

    [Fact]
    public async Task IsRevoked_NoSharedCache_ExpiredRow_IsNotRevoked()
    {
        var sut = CreateSut(InProcessDistributedCache());

        _db.RevokedTokens.Add(new RevokedToken { Jti = "jti-7", ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        await _db.SaveChangesAsync();

        (await sut.IsRevokedAsync("jti-7")).Should().BeFalse();
    }
}
