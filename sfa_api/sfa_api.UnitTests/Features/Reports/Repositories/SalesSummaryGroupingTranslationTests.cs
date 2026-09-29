using System.Data.Common;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using sfa_api.Features.Reports.Enums;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Requests;
using sfa_api.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace sfa_api.UnitTests.Features.Reports.Repositories;

/// <summary>
/// Multi-dimension grouping builds its GROUP BY key as an expression tree. SQLite can't run the
/// report's decimal SUMs, so these prove on the real Npgsql provider that the composite key
/// translates to ONE grouped query over plain columns — no client evaluation, no constants.
/// Same capture-and-abort technique as PricingQueryTranslationTests.
/// </summary>
public class SalesSummaryGroupingTranslationTests(ITestOutputHelper output)
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

    private async Task<string> SqlOf(Func<Task> query)
    {
        var act = async () => await query();
        var ex = await act.Should().ThrowAsync<CapturedSqlException>();
        output.WriteLine(ex.Which.Sql);
        return ex.Which.Sql;
    }

    private static string GroupByClause(string sql)
        => Regex.Match(sql, @"GROUP BY (.+?)(\r?\n|LIMIT|$)", RegexOptions.Singleline).Groups[1].Value;

    private static SalesSummaryQuery Query(params SalesSummaryGroupBy[] dims)
        => new(dims[0], new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), ThenBy: dims[1..]);

    [Fact]
    public async Task Sales_SingleDimension_GroupsByThatColumnOnly()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new SalesSummaryRepository(db)
            .GetSalesAggregatesAsync(Query(SalesSummaryGroupBy.SalesRep), maxGroups: 100));

        var groupBy = GroupByClause(sql);
        groupBy.Should().Contain("\"SalesRepId\"");
        groupBy.Should().NotContain("NULL");
    }

    [Fact]
    public async Task Sales_ThreeDimensions_OneQueryGroupedByAllThreeColumns()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new SalesSummaryRepository(db).GetSalesAggregatesAsync(
            Query(SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Territory, SalesSummaryGroupBy.Distributor),
            maxGroups: 100));

        var groupBy = GroupByClause(sql);
        groupBy.Should().Contain("\"SalesRepId\"")
            .And.Contain("\"TerritoryId\"")
            .And.Contain("\"DistributorId\"")
            .And.NotContain("NULL");
    }

    [Fact]
    public async Task Sales_ProductWithGeo_MixesItemAndHeaderColumns()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new SalesSummaryRepository(db).GetSalesAggregatesAsync(
            Query(SalesSummaryGroupBy.Area, SalesSummaryGroupBy.Product), maxGroups: 100));

        GroupByClause(sql).Should().Contain("\"AreaId\"").And.Contain("\"ProductId\"");
    }

    [Fact]
    public async Task Targets_ThreeDimensions_Translate()
    {
        await using var db = NpgsqlContext();
        var sql = await SqlOf(() => new SalesSummaryRepository(db).GetTargetAggregatesAsync(
            Query(SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Territory, SalesSummaryGroupBy.Distributor),
            [(2026, 9)], maxGroups: 100));

        GroupByClause(sql).Should().Contain("\"SalesRepId\"")
            .And.Contain("\"TerritoryId\"")
            .And.Contain("\"DistributorId\"");
    }

    [Fact]
    public async Task Targets_WithRouteDimension_AreNotQueried()
    {
        await using var db = NpgsqlContext();

        var rows = await new SalesSummaryRepository(db).GetTargetAggregatesAsync(
            Query(SalesSummaryGroupBy.SalesRep, SalesSummaryGroupBy.Route), [(2026, 9)], maxGroups: 100);

        rows.Should().BeEmpty("SalesTarget has no RouteId, so no query is issued at all");
    }
}
