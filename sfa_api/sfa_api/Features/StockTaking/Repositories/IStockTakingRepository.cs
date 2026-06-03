using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.StockTaking.Entities;

namespace sfa_api.Features.StockTaking.Repositories;

public interface IStockTakingRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
    // ── Periods ───────────────────────────────────────────────────────────
    Task<(List<StockTakingPeriod> Items, int TotalCount)> GetPeriodsAsync(
        int skip, int take, string? search, CancellationToken ct = default);

    Task<StockTakingPeriod?> GetPeriodByIdAsync(int id, CancellationToken ct = default);

    Task<bool> PeriodExistsAsync(int month, int year, CancellationToken ct = default);

    Task<List<StockTakingPeriod>> GetOpenPeriodsAsync(CancellationToken ct = default);

    Task<StockTakingPeriod> CreatePeriodAsync(StockTakingPeriod period, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    // ── Submissions ───────────────────────────────────────────────────────
    Task<StockTakingSubmission?> GetSubmissionAsync(
        int periodId, int distributorId, CancellationToken ct = default);

    Task<StockTakingSubmission?> GetSubmissionByIdAsync(int id, CancellationToken ct = default);

    Task<StockTakingSubmission> UpsertSubmissionAsync(
        StockTakingSubmission submission, CancellationToken ct = default);

    // ── Lines ─────────────────────────────────────────────────────────────
    Task<StockTakingLine?> GetLineByIdAsync(int id, CancellationToken ct = default);
}
