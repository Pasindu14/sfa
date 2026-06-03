using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.StockTaking.DTOs;
using sfa_api.Features.StockTaking.Entities;
using sfa_api.Features.StockTaking.Enums;
using sfa_api.Features.StockTaking.Repositories;
using sfa_api.Features.StockTaking.Requests;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.StockTaking.Services;

public class StockTakingService(
    IStockTakingRepository repo,
    IStockRepository stockRepo,
    IUserRepository userRepo,
    IDistributedLockService lockService,
    AppDbContext db) : IStockTakingService
{
    private readonly IStockTakingRepository _repo         = repo;
    private readonly IStockRepository       _stockRepo    = stockRepo;
    private readonly IUserRepository        _userRepo     = userRepo;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly AppDbContext           _db           = db;

    // ── Admin ─────────────────────────────────────────────────────────────

    public async Task<(List<StockTakingPeriodDto> Items, int TotalCount)> GetPeriodsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (items, total) = await _repo.GetPeriodsAsync(skip, pageSize, search, ct);
        return (items.Select(MapPeriod).ToList(), total);
    }

    public async Task<StockTakingPeriodDto> GetPeriodByIdAsync(int id, CancellationToken ct = default)
    {
        var period = await _repo.GetPeriodByIdAsync(id, ct)
            ?? throw new NotFoundException("STOCK_TAKING_PERIOD_NOT_FOUND", $"Period {id} not found.");
        return MapPeriod(period);
    }

    public async Task<StockTakingPeriodDto> CreatePeriodAsync(
        CreatePeriodRequest request, int createdBy, CancellationToken ct = default)
    {
        if (await _repo.PeriodExistsAsync(request.Month, request.Year, ct))
            throw new DuplicateResourceException("StockTakingPeriod");

        var period = new StockTakingPeriod
        {
            Month     = request.Month,
            Year      = request.Year,
            Status    = StockTakingPeriodStatus.Open,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        var created = await _repo.CreatePeriodAsync(period, ct);
        return MapPeriod(created);
    }

    public async Task<StockTakingPeriodDto> LockPeriodAsync(
        int id, int lockedBy, CancellationToken ct = default)
    {
        var period = await GetTrackedPeriodAsync(id, ct);

        if (period.Status == StockTakingPeriodStatus.Locked)
            throw new BusinessRuleException("STOCK_TAKING_PERIOD_ALREADY_LOCKED",
                "This period is already locked.");

        period.Status    = StockTakingPeriodStatus.Locked;
        period.LockedAt  = DateTime.UtcNow;
        period.LockedBy  = lockedBy;
        period.UpdatedAt = DateTime.UtcNow;
        period.UpdatedBy = lockedBy;
        await _repo.SaveChangesAsync(ct);
        return MapPeriod(period);
    }

    public async Task<StockTakingPeriodDto> UnlockPeriodAsync(
        int id, int unlockedBy, CancellationToken ct = default)
    {
        var period = await GetTrackedPeriodAsync(id, ct);

        if (period.Status == StockTakingPeriodStatus.Open)
            throw new BusinessRuleException("STOCK_TAKING_PERIOD_ALREADY_OPEN",
                "This period is already open.");

        period.Status    = StockTakingPeriodStatus.Open;
        period.LockedAt  = null;
        period.LockedBy  = null;
        period.UpdatedAt = DateTime.UtcNow;
        period.UpdatedBy = unlockedBy;
        await _repo.SaveChangesAsync(ct);
        return MapPeriod(period);
    }

    public async Task<StockTakingSubmissionDto> GetSubmissionForAdminAsync(
        int periodId, int distributorId, CancellationToken ct = default)
    {
        var submission = await _repo.GetSubmissionAsync(periodId, distributorId, ct)
            ?? throw new NotFoundException("STOCK_TAKING_SUBMISSION_NOT_FOUND",
                "No submission found for this distributor and period.");
        return MapSubmission(submission);
    }

    public async Task<StockTakingLineDto> AdjustLineAsync(
        int lineId, AdjustLineRequest request, int adminUserId, CancellationToken ct = default)
    {
        var line = await _repo.GetLineByIdAsync(lineId, ct)
            ?? throw new NotFoundException("STOCK_TAKING_LINE_NOT_FOUND", $"Line {lineId} not found.");

        var distributorId = line.Submission.DistributorId;
        var productId     = line.ProductId;
        var stockType     = line.StockType;

        await using var advisoryLock = await _lockService.AcquireAsync(
            $"stock-adjust:{distributorId}:{productId}:{stockType}", ct)
            ?? throw new BusinessRuleException("STOCK_LOCK_FAILED",
                "Could not acquire stock lock. Another operation is in progress.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _repo.BeginTransactionAsync(ct);
            try
            {
                var stock = await _stockRepo.GetStockForUpdateAsync(
                    distributorId, productId, stockType, ct);

                var currentQoh = stock?.QuantityOnHand ?? 0m;
                var delta      = request.AdjustedQuantity - currentQoh;

                if (delta > 0)
                {
                    await _stockRepo.CreditStockAsync(
                        distributorId, productId, delta, stockType,
                        StockTransactionType.StockTakingAdjustment,
                        referenceType: "StockTaking", referenceId: lineId,
                        transactedBy: adminUserId, ct: ct);
                }
                else if (delta < 0)
                {
                    await _stockRepo.DeductStockAsync(
                        distributorId, productId, Math.Abs(delta), stockType,
                        StockTransactionType.StockTakingAdjustment,
                        referenceType: "StockTaking", referenceId: lineId,
                        transactedBy: adminUserId, ct: ct);
                }

                line.IsAdjusted       = true;
                line.AdjustedQuantity = request.AdjustedQuantity;
                line.AdjustedBy       = adminUserId;
                line.AdjustedAt       = DateTime.UtcNow;
                await _stockRepo.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return MapLine(line);
    }

    // ── Distributor portal ────────────────────────────────────────────────

    public async Task<List<StockTakingPeriodDto>> GetOpenPeriodsAsync(CancellationToken ct = default)
    {
        var periods = await _repo.GetOpenPeriodsAsync(ct);
        return periods.Select(MapPeriod).ToList();
    }

    public async Task<StockTakingSubmissionDto?> GetMySubmissionAsync(
        int periodId, int distributorId, CancellationToken ct = default)
    {
        var submission = await _repo.GetSubmissionAsync(periodId, distributorId, ct);
        return submission == null ? null : MapSubmission(submission);
    }

    public async Task<StockTakingSubmissionDto> UpsertDraftAsync(
        UpsertSubmissionRequest request, int distributorId, int userId, CancellationToken ct = default)
    {
        var period = await _repo.GetPeriodByIdAsync(request.PeriodId, ct)
            ?? throw new NotFoundException("STOCK_TAKING_PERIOD_NOT_FOUND",
                $"Period {request.PeriodId} not found.");

        EnsurePeriodOpen(period);

        var existing = await _repo.GetSubmissionAsync(request.PeriodId, distributorId, ct);

        if (existing != null)
        {
            // Replace all lines with the new cart state
            existing.Lines.Clear();
            foreach (var item in request.Lines)
                existing.Lines.Add(BuildLine(item));

            existing.Status    = StockTakingSubmissionStatus.Draft;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
            await _repo.UpsertSubmissionAsync(existing, ct);
            return MapSubmission(existing);
        }

        var submission = new StockTakingSubmission
        {
            StockTakingPeriodId = request.PeriodId,
            DistributorId       = distributorId,
            Status              = StockTakingSubmissionStatus.Draft,
            CreatedBy           = userId,
            UpdatedBy           = userId,
            Lines               = request.Lines.Select(BuildLine).ToList()
        };

        await _repo.UpsertSubmissionAsync(submission, ct);
        return MapSubmission(submission);
    }

    public async Task<StockTakingSubmissionDto> SubmitAsync(
        int periodId, int distributorId, int userId, CancellationToken ct = default)
    {
        var period = await _repo.GetPeriodByIdAsync(periodId, ct)
            ?? throw new NotFoundException("STOCK_TAKING_PERIOD_NOT_FOUND",
                $"Period {periodId} not found.");

        EnsurePeriodOpen(period);

        var submission = await GetTrackedSubmissionAsync(periodId, distributorId, ct);

        // Snapshot system stock for every line at this moment
        foreach (var line in submission.Lines)
        {
            var stock = await _stockRepo.GetStockForUpdateAsync(
                distributorId, line.ProductId, line.StockType, ct);

            line.SystemQuantity = stock?.QuantityOnHand ?? 0m;
            line.Variance       = line.CountedQuantity - line.SystemQuantity;
        }

        submission.Status      = StockTakingSubmissionStatus.Submitted;
        submission.SubmittedAt = DateTime.UtcNow;
        submission.SubmittedBy = userId;
        submission.UpdatedAt   = DateTime.UtcNow;
        submission.UpdatedBy   = userId;

        await _repo.SaveChangesAsync(ct);
        return MapSubmission(submission);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<StockTakingPeriod> GetTrackedPeriodAsync(int id, CancellationToken ct)
    {
        return await _db.StockTakingPeriods
            .FindAsync([id], ct)
            ?? throw new NotFoundException("STOCK_TAKING_PERIOD_NOT_FOUND", $"Period {id} not found.");
    }

    private async Task<StockTakingSubmission> GetTrackedSubmissionAsync(
        int periodId, int distributorId, CancellationToken ct)
    {
        return await _db.StockTakingSubmissions
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s =>
                s.StockTakingPeriodId == periodId &&
                s.DistributorId == distributorId &&
                !s.IsDeleted, ct)
            ?? throw new NotFoundException("STOCK_TAKING_SUBMISSION_NOT_FOUND",
                "No draft submission found. Please save a draft first.");
    }

    private static void EnsurePeriodOpen(StockTakingPeriod period)
    {
        if (period.Status == StockTakingPeriodStatus.Locked)
            throw new BusinessRuleException("STOCK_TAKING_PERIOD_LOCKED",
                "This stock taking period is locked. No changes are allowed.");
    }

    private static StockTakingLine BuildLine(UpsertSubmissionLineItem item)
    {
        var stockType = Enum.TryParse<StockType>(item.StockType, out var st)
            ? st
            : StockType.Normal;

        return new StockTakingLine
        {
            ProductId       = item.ProductId,
            StockType       = stockType,
            CountedQuantity = item.CountedQuantity
        };
    }

    // ── Mapping ───────────────────────────────────────────────────────────

    private static StockTakingPeriodDto MapPeriod(StockTakingPeriod p) => new(
        p.Id, p.Month, p.Year, p.Status.ToString(),
        p.LockedAt, p.LockedBy, p.LockedByUser?.Name,
        p.IsActive, p.CreatedAt, p.UpdatedAt);

    private static StockTakingSubmissionDto MapSubmission(StockTakingSubmission s) => new(
        s.Id, s.StockTakingPeriodId,
        s.Period?.Month ?? 0, s.Period?.Year ?? 0,
        s.DistributorId, s.Distributor?.Name ?? string.Empty,
        s.Status.ToString(),
        s.SubmittedAt, s.SubmittedBy, s.SubmittedByUser?.Name,
        s.CreatedAt, s.UpdatedAt,
        s.Lines.Select(MapLine).ToList());

    private static StockTakingLineDto MapLine(StockTakingLine l) => new(
        l.Id, l.StockTakingSubmissionId,
        l.ProductId, l.Product?.Code ?? string.Empty,
        l.Product?.ItemDescription ?? string.Empty,
        l.StockType.ToString(),
        l.CountedQuantity, l.SystemQuantity, l.Variance,
        l.IsAdjusted, l.AdjustedQuantity,
        l.AdjustedBy, l.AdjustedByUser?.Name, l.AdjustedAt);
}
