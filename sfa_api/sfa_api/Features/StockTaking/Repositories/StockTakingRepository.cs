using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using sfa_api.Features.StockTaking.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.StockTaking.Repositories;

public class StockTakingRepository(AppDbContext db) : IStockTakingRepository
{
    private readonly AppDbContext _db = db;

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _db.Database.BeginTransactionAsync(ct);

    // ── Periods ───────────────────────────────────────────────────────────

    public async Task<(List<StockTakingPeriod> Items, int TotalCount)> GetPeriodsAsync(
        int skip, int take, string? search, CancellationToken ct = default)
    {
        var query = _db.StockTakingPeriods
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search) && int.TryParse(search, out var year))
            query = query.Where(p => p.Year == year);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Skip(skip)
            .Take(take)
            .Include(p => p.LockedByUser)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<StockTakingPeriod?> GetPeriodByIdAsync(int id, CancellationToken ct = default)
        => await _db.StockTakingPeriods
            .AsNoTracking()
            .Include(p => p.LockedByUser)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public async Task<bool> PeriodExistsAsync(int month, int year, CancellationToken ct = default)
        => await _db.StockTakingPeriods
            .AsNoTracking()
            .AnyAsync(p => p.Month == month && p.Year == year && !p.IsDeleted, ct);

    public async Task<List<StockTakingPeriod>> GetOpenPeriodsAsync(CancellationToken ct = default)
        => await _db.StockTakingPeriods
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status == Enums.StockTakingPeriodStatus.Open)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .ToListAsync(ct);

    public async Task<StockTakingPeriod> CreatePeriodAsync(
        StockTakingPeriod period, CancellationToken ct = default)
    {
        _db.StockTakingPeriods.Add(period);
        await _db.SaveChangesAsync(ct);
        return period;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    // ── Submissions ───────────────────────────────────────────────────────

    public async Task<StockTakingSubmission?> GetSubmissionAsync(
        int periodId, int distributorId, CancellationToken ct = default)
        => await _db.StockTakingSubmissions
            .AsNoTracking()
            .Include(s => s.Lines)
                .ThenInclude(l => l.Product)
            .Include(s => s.SubmittedByUser)
            .Include(s => s.Distributor)
            .Include(s => s.Period)
            .FirstOrDefaultAsync(s =>
                s.StockTakingPeriodId == periodId &&
                s.DistributorId == distributorId &&
                !s.IsDeleted, ct);

    public async Task<StockTakingSubmission?> GetSubmissionByIdAsync(int id, CancellationToken ct = default)
        => await _db.StockTakingSubmissions
            .AsNoTracking()
            .Include(s => s.Lines)
                .ThenInclude(l => l.Product)
            .Include(s => s.SubmittedByUser)
            .Include(s => s.Distributor)
            .Include(s => s.Period)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

    public async Task<StockTakingSubmission> UpsertSubmissionAsync(
        StockTakingSubmission submission, CancellationToken ct = default)
    {
        if (submission.Id == 0)
        {
            _db.StockTakingSubmissions.Add(submission);
        }
        else
        {
            _db.StockTakingSubmissions.Update(submission);
        }
        await _db.SaveChangesAsync(ct);
        return submission;
    }

    // ── Lines ─────────────────────────────────────────────────────────────

    public async Task<StockTakingLine?> GetLineByIdAsync(int id, CancellationToken ct = default)
        => await _db.StockTakingLines
            .Include(l => l.Submission)
                .ThenInclude(s => s.Period)
            .Include(l => l.Submission)
                .ThenInclude(s => s.Distributor)
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
}
