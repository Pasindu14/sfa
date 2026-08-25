using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.SalesTargets.DTOs;
using sfa_api.Features.SalesTargets.Entities;
using sfa_api.Features.SalesTargets.Enums;
using sfa_api.Features.SalesTargets.Repositories;
using sfa_api.Features.SalesTargets.Requests;
using sfa_api.Features.SalesTargets.Services;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.UnitTests.Features.SalesTargets.Services;

/// <summary>
/// Covers the two failure modes that stranded the August 2026 import: a rep/product pair
/// repeated inside one file (which used to violate the DB unique index and lose the whole
/// batch), and a batch left at Processing forever when the write threw.
/// </summary>
public class SalesTargetImportServiceTests : IDisposable
{
    private const int CallerId  = 42;
    private const int RepId     = 15;
    private const int ProductId = 6;
    private const int Year      = 2026;
    private const int Month     = 8;

    private readonly Mock<ISalesTargetRepository> _targetRepoMock = new();
    private readonly Mock<ISalesTargetImportBatchRepository> _batchRepoMock = new();
    private readonly Mock<IUserReportingLineRepository> _reportingLineRepoMock = new();
    private readonly Mock<IUserGeoAssignmentRepository> _geoRepoMock = new();
    private readonly Mock<IDistributorRepository> _distributorRepoMock = new();
    private readonly AppDbContext _dbContext;
    private readonly SalesTargetImportService _sut;

    private SalesTargetImportBatch? _createdBatch;
    private List<SalesTarget> _added = [];
    private List<SalesTarget> _updated = [];

    public SalesTargetImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new SqliteFriendlyDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _dbContext.Users.Add(new User
        {
            Id = RepId, Name = "Test Rep", Username = "rep15",
            Email = "rep15@test.local", Phone = "0770000000", PasswordHash = "x",
            Role = UserRole.SalesRep, IsActive = true,
        });
        _dbContext.Products.Add(new Product
        {
            Id = ProductId, Code = "CC03",
            ItemDescription = "Real Cream Cracker 490g X 4Pktsx Rs400",
            IsActive = true,
        });
        _dbContext.SaveChanges();

        _batchRepoMock.Setup(r => r.GetNextBatchNumberAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(8L);
        _batchRepoMock.Setup(r => r.CreateAsync(It.IsAny<SalesTargetImportBatch>(), It.IsAny<CancellationToken>()))
                      .Callback<SalesTargetImportBatch, CancellationToken>((b, _) => _createdBatch = b)
                      .Returns(Task.CompletedTask);
        _batchRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

        _targetRepoMock.Setup(r => r.GetExistingForMonthAsync(
                            It.IsAny<int>(), It.IsAny<int>(),
                            It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<int>>(),
                            It.IsAny<CancellationToken>()))
                       .ReturnsAsync([]);
        _targetRepoMock.Setup(r => r.AddRange(It.IsAny<IEnumerable<SalesTarget>>()))
                       .Callback<IEnumerable<SalesTarget>>(t => _added = t.ToList());
        _targetRepoMock.Setup(r => r.UpdateRange(It.IsAny<IEnumerable<SalesTarget>>()))
                       .Callback<IEnumerable<SalesTarget>>(t => _updated = t.ToList());
        _targetRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);

        _reportingLineRepoMock.Setup(r => r.GetActiveLinesForUsersAsync(
                                  It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                              .ReturnsAsync([]);

        _sut = new SalesTargetImportService(
            _targetRepoMock.Object,
            _batchRepoMock.Object,
            _reportingLineRepoMock.Object,
            _geoRepoMock.Object,
            _distributorRepoMock.Object,
            _dbContext,
            NullLogger<SalesTargetImportService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ImportSalesTargetsRequest Request(params TargetRowRequest[] rows)
        => new("targets.xlsx", Year, Month, rows.ToList());

    // ── In-file duplicates ────────────────────────────────────────────────

    [Fact]
    public async Task ImportAsync_WhenSameRepAndProductAppearTwice_StagesOneRowWithTheLastQuantity()
    {
        // The August file listed CC03 twice per rep: once with 0, then with the real target.
        var result = await _sut.ImportAsync(Request(
            new TargetRowRequest(1, RepId, "CC03", 0m),
            new TargetRowRequest(5, RepId, "CC03", 160m)), CallerId);

        _added.Should().ContainSingle("the DB unique index allows one row per rep+product+period");
        _added[0].TargetQuantity.Should().Be(160m, "the last row in the file wins");
        result.InsertedRows.Should().Be(1);
        result.SkippedRows.Should().Be(1);
        result.TotalRows.Should().Be(2);
    }

    [Fact]
    public async Task ImportAsync_WhenSameRepAndProductAppearTwice_ReportsTheSupersededRow()
    {
        var result = await _sut.ImportAsync(Request(
            new TargetRowRequest(1, RepId, "CC03", 0m),
            new TargetRowRequest(5, RepId, "CC03", 160m)), CallerId);

        var error = result.Errors.Should().ContainSingle().Subject;
        error.RowIndex.Should().Be(1, "row 1 is the one whose quantity was discarded");
        error.ItemCode.Should().Be("CC03");
        error.Reason.Should().Contain("superseded by row 5");
    }

    [Fact]
    public async Task ImportAsync_WhenDuplicateRowsAreCollapsed_CountersStillSumToTotalRows()
    {
        var result = await _sut.ImportAsync(Request(
            new TargetRowRequest(1, RepId, "CC03", 0m),
            new TargetRowRequest(5, RepId, "CC03", 160m),
            new TargetRowRequest(9, RepId, "NOPE", 20m)), CallerId);

        (result.InsertedRows + result.UpdatedRows + result.SkippedRows)
            .Should().Be(result.TotalRows);
        result.Status.Should().Be(SalesTargetImportBatchStatus.PartialFailed);
    }

    [Fact]
    public async Task ImportAsync_WhenDuplicateHitsAnExistingTarget_UpdatesItOnceWithTheLastQuantity()
    {
        var existing = new SalesTarget
        {
            Id = 99, Year = Year, Month = Month,
            SalesRepId = RepId, ProductId = ProductId, TargetQuantity = 5m,
        };
        _targetRepoMock.Setup(r => r.GetExistingForMonthAsync(
                            It.IsAny<int>(), It.IsAny<int>(),
                            It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<int>>(),
                            It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new Dictionary<(int SalesRepId, int ProductId), SalesTarget>
                       {
                           [(RepId, ProductId)] = existing,
                       });

        var result = await _sut.ImportAsync(Request(
            new TargetRowRequest(1, RepId, "CC03", 0m),
            new TargetRowRequest(5, RepId, "CC03", 160m)), CallerId);

        _updated.Should().ContainSingle();
        existing.TargetQuantity.Should().Be(160m);
        result.UpdatedRows.Should().Be(1);
        result.SkippedRows.Should().Be(1);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithNoDuplicates_CompletesWithoutSkips()
    {
        var result = await _sut.ImportAsync(Request(
            new TargetRowRequest(1, RepId, "CC03", 160m)), CallerId);

        result.Status.Should().Be(SalesTargetImportBatchStatus.Completed);
        result.InsertedRows.Should().Be(1);
        result.SkippedRows.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    // ── Batch never stranded at Processing ────────────────────────────────

    [Fact]
    public async Task ImportAsync_WhenTargetWriteThrows_MarksBatchFailedAndRethrows()
    {
        _targetRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException("insert blew up"));

        var act = async () => await _sut.ImportAsync(
            Request(new TargetRowRequest(1, RepId, "CC03", 160m)), CallerId);

        await act.Should().ThrowAsync<DbUpdateException>();

        _createdBatch.Should().NotBeNull();
        _createdBatch!.Status.Should().Be(SalesTargetImportBatchStatus.Failed,
            "a crashed batch must never be left indistinguishable from one still running");
        _createdBatch.InsertedRows.Should().Be(0);
        _createdBatch.UpdatedRows.Should().Be(0);
        _createdBatch.SkippedRows.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_WhenTargetWriteThrows_StoresASafeReasonWithoutRawExceptionText()
    {
        _targetRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException("connection string: super-secret"));

        var act = async () => await _sut.ImportAsync(
            Request(new TargetRowRequest(1, RepId, "CC03", 160m)), CallerId);
        await act.Should().ThrowAsync<DbUpdateException>();

        _createdBatch!.ErrorSummary.Should().NotBeNull();
        _createdBatch.ErrorSummary.Should().NotContain("super-secret");

        var errors = JsonSerializer.Deserialize<List<SalesTargetImportErrorDto>>(_createdBatch.ErrorSummary!);
        errors.Should().ContainSingle();
        errors![0].Reason.Should().Contain("No targets were written");
    }

    [Fact]
    public async Task ImportAsync_WhenRequestIsCancelledMidRun_StillMarksBatchFailed()
    {
        using var cts = new CancellationTokenSource();
        _targetRepoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .Callback(cts.Cancel)
                       .ThrowsAsync(new OperationCanceledException());

        var act = async () => await _sut.ImportAsync(
            Request(new TargetRowRequest(1, RepId, "CC03", 160m)), CallerId, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        _createdBatch!.Status.Should().Be(SalesTargetImportBatchStatus.Failed);
        // The bookkeeping write must not ride on the cancelled request token.
        _batchRepoMock.Verify(r => r.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    /// <summary>
    /// SQLite has no sequences and no xmin system column; patch both so EnsureCreated and the
    /// User/Product inserts this fixture needs will work. Mirrors the integration-test context.
    /// </summary>
    private class SqliteFriendlyDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var seq in modelBuilder.Model.GetSequences().ToList())
                modelBuilder.Model.RemoveSequence(seq.Name, seq.Schema);

            modelBuilder.Entity<User>()
                .Property(x => x.RowVersion)
                .HasColumnType("INTEGER").HasDefaultValue(1u)
                .ValueGeneratedOnAdd().IsConcurrencyToken(false);

            modelBuilder.Entity<Product>()
                .Property(x => x.RowVersion)
                .HasColumnType("INTEGER").HasDefaultValue(1u)
                .ValueGeneratedOnAdd().IsConcurrencyToken(false);
        }
    }
}
