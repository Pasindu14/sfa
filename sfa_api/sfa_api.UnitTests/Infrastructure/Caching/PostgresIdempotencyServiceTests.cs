using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using sfa_api.Common.Audit;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Infrastructure.Caching;

public sealed class PostgresIdempotencyServiceTests : IDisposable
{
    private const string Key = "POST:/api/v1/stock-transfers:1:abc";

    private readonly AppDbContext _arrangeDb;
    private readonly List<AppDbContext> _extraContexts = [];

    public PostgresIdempotencyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _arrangeDb = new AppDbContext(options);
        _arrangeDb.Database.OpenConnection();
        // EnsureCreated() can't run (PostgreSQL sequences) — create just the table under test.
        _arrangeDb.Database.ExecuteSqlRaw(
            "CREATE TABLE \"IdempotencyKeys\" (\"Key\" TEXT NOT NULL PRIMARY KEY, \"ResponseJson\" TEXT NOT NULL, " +
            "\"StatusCode\" INTEGER NOT NULL, \"CreatedAt\" TEXT NOT NULL, \"ExpiresAt\" TEXT NOT NULL)");
    }

    public void Dispose()
    {
        foreach (var c in _extraContexts) c.Dispose();
        _arrangeDb.Dispose();
    }

    /// <summary>A fresh context on the same in-memory connection, so each call sees only what was committed (like separate requests).</summary>
    private AppDbContext NewContext()
    {
        var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_arrangeDb.Database.GetDbConnection())
            .Options);
        _extraContexts.Add(ctx);
        return ctx;
    }

    private PostgresIdempotencyService CreateSut()
        => new(NewContext(), NullLogger<PostgresIdempotencyService>.Instance);

    private async Task<IdempotencyKey> RowAsync(string key = Key)
        => await NewContext().IdempotencyKeys.AsNoTracking().SingleAsync(x => x.Key == key);

    private async Task SeedRowAsync(DateTime expiresAt, string json = "{\"old\":true}", int status = 201)
    {
        _arrangeDb.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = Key, ResponseJson = json, StatusCode = status,
            CreatedAt = DateTime.UtcNow.AddDays(-2), ExpiresAt = expiresAt
        });
        await _arrangeDb.SaveChangesAsync();
        _arrangeDb.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Store_NewKey_GetReturnsTheStoredResponse()
    {
        await CreateSut().StoreAsync(Key, 201, "{\"id\":1}");

        var hit = await CreateSut().GetAsync(Key);

        hit.Should().NotBeNull();
        hit!.StatusCode.Should().Be(201);
        hit.ResponseJson.Should().Be("{\"id\":1}");
        (await RowAsync()).ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Get_UnknownKey_ReturnsNull()
    {
        (await CreateSut().GetAsync("missing")).Should().BeNull();
    }

    [Fact]
    public async Task Store_LiveRowAlreadyExists_LeavesItUnchanged()
    {
        var originalExpiry = DateTime.UtcNow.AddHours(10);
        await SeedRowAsync(originalExpiry, json: "{\"first\":true}", status: 201);

        await CreateSut().StoreAsync(Key, 200, "{\"second\":true}");

        var row = await RowAsync();
        row.ResponseJson.Should().Be("{\"first\":true}");
        row.StatusCode.Should().Be(201);
        row.ExpiresAt.Should().BeCloseTo(originalExpiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Store_ExpiredRowStillPresent_RefreshesItInPlace()
    {
        await SeedRowAsync(DateTime.UtcNow.AddMinutes(-30), json: "{\"old\":true}", status: 201);
        (await CreateSut().GetAsync(Key)).Should().BeNull("an expired row is not a cache hit");

        await CreateSut().StoreAsync(Key, 200, "{\"new\":true}");

        var row = await RowAsync();
        row.ResponseJson.Should().Be("{\"new\":true}");
        row.StatusCode.Should().Be(200);
        row.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));

        var hit = await CreateSut().GetAsync(Key);
        hit.Should().NotBeNull();
        hit!.ResponseJson.Should().Be("{\"new\":true}");
        hit.StatusCode.Should().Be(200);

        (await NewContext().IdempotencyKeys.CountAsync(x => x.Key == Key)).Should().Be(1, "refreshed, not duplicated");
    }

    [Fact]
    public async Task Store_SameKeyTwiceInARow_SecondIsANoOp()
    {
        await CreateSut().StoreAsync(Key, 201, "{\"a\":1}");

        await CreateSut().StoreAsync(Key, 201, "{\"b\":2}");

        (await RowAsync()).ResponseJson.Should().Be("{\"a\":1}");
    }
}
