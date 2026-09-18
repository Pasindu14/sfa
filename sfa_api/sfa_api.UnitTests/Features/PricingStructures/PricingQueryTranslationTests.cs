using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using sfa_api.Features.MobileSync.Repositories;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Requests;
using sfa_api.Features.SalesTargets.Repositories;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.PricingStructures;

/// <summary>
/// Proves the default-structure price queries TRANSLATE on the real PostgreSQL provider. The
/// integration suite runs on SQLite, which can't evaluate decimal SUM at all, so a query that EF/Npgsql
/// refuses to translate (a correlated subquery inside a grouped SUM, a left join through the
/// <c>DefaultPricingItems()</c> helper) would otherwise only fail in production.
/// <para>
/// No database is involved: an interceptor pretends the connection opened and aborts the command
/// with the generated SQL, so the test asserts translation, not results.
/// </para>
/// </summary>
public class PricingQueryTranslationTests
{
    private sealed class CapturedSqlException(string sql) : Exception(sql)
    {
        public string Sql { get; } = sql;
    }

    private sealed class CaptureInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => throw new CapturedSqlException(command.CommandText);

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
            => throw new CapturedSqlException(command.CommandText);
    }

    private sealed class FakeOpenInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private static AppDbContext NpgsqlContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=translation-only.invalid;Database=none")
            .AddInterceptors(new FakeOpenInterceptor(), new CaptureInterceptor())
            .Options);

    private static async Task<string> SqlOf(Func<Task> query)
    {
        var act = async () => await query();
        // Anything but CapturedSqlException (e.g. InvalidOperationException "could not be translated")
        // fails the test with the real message.
        var ex = await act.Should().ThrowAsync<CapturedSqlException>();
        return ex.Which.Sql;
    }

    [Fact]
    public async Task MobileProductSync_LegacyPrices_LeftJoinDefaultStructure()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new MobileSyncRepository(db).GetActiveProductsAsync());
        sql.Should().Contain("PricingStructureItems").And.Contain("LEFT JOIN");
    }

    [Fact]
    public async Task MobilePricingStructureSync_Translates()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new MobileSyncRepository(db).GetActivePricingStructuresAsync());
        sql.Should().Contain("PricingStructures");
    }

    [Fact]
    public async Task BinCardProducts_DefaultPackPrice_Translates()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new BinCardRepository(db).GetBinCardProductsAsync([1, 2]));
        sql.Should().Contain("PricingStructureItems");
    }

    [Fact]
    public async Task RepMonthlyTargetValue_DefaultCasePrice_Translates()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new SalesTargetRepository(db).GetRepMonthlyTargetValueAsync(1, 2026, 9));
        sql.Should().Contain("PricingStructureItems");
    }

    [Fact]
    public async Task SalesSummaryTargets_SubqueryInsideGroupedSum_Translates()
    {
        await using var db = NpgsqlContext();
        var query = new SalesSummaryQuery(SalesSummaryGroupBy.SalesRep, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));
        var sql = await SqlOf(() => new SalesSummaryRepository(db)
            .GetTargetAggregatesAsync(query, [(2026, 9)], maxGroups: 100));
        sql.Should().Contain("PricingStructureItems").And.Contain("GROUP BY");
    }

    [Fact]
    public async Task PricingStructureList_WithPricedCount_Translates()
    {
        await using var db = NpgsqlContext();
        // The first command is the COUNT; that proves the filter. Run the page query alone too.
        var sql = await SqlOf(() => new PricingStructureRepository(db).GetAllAsync(0, 10, "std", true));
        sql.Should().Contain("PricingStructures");
    }

    [Fact]
    public async Task PricingStructureItemRows_LeftJoinAllProducts_Translates()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new PricingStructureRepository(db).GetItemRowsAsync(1));
        sql.Should().Contain("LEFT JOIN").And.Contain("Products");
    }

    [Fact]
    public async Task DefaultPrices_Translates()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new PricingStructureRepository(db).GetDefaultPricesAsync());
        sql.Should().Contain("IsDefault");
    }
}
