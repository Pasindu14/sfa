using sfa_api.Features.StockTaking.DTOs;
using sfa_api.Features.StockTaking.Requests;

namespace sfa_api.Features.StockTaking.Services;

public interface IStockTakingService
{
    // ── Admin ─────────────────────────────────────────────────────────────
    Task<(List<StockTakingPeriodDto> Items, int TotalCount)> GetPeriodsAsync(
        int page, int pageSize, string? search, CancellationToken ct = default);

    Task<StockTakingPeriodDto> GetPeriodByIdAsync(int id, CancellationToken ct = default);

    Task<StockTakingPeriodDto> CreatePeriodAsync(
        CreatePeriodRequest request, int createdBy, CancellationToken ct = default);

    Task<StockTakingPeriodDto> LockPeriodAsync(
        int id, int lockedBy, CancellationToken ct = default);

    Task<StockTakingPeriodDto> UnlockPeriodAsync(
        int id, int unlockedBy, CancellationToken ct = default);

    Task<StockTakingSubmissionDto> GetSubmissionForAdminAsync(
        int periodId, int distributorId, CancellationToken ct = default);

    Task<StockTakingLineDto> AdjustLineAsync(
        int lineId, AdjustLineRequest request, int adminUserId, CancellationToken ct = default);

    // ── Distributor portal ────────────────────────────────────────────────
    Task<List<StockTakingPeriodDto>> GetOpenPeriodsAsync(CancellationToken ct = default);

    Task<StockTakingSubmissionDto?> GetMySubmissionAsync(
        int periodId, int distributorId, CancellationToken ct = default);

    Task<StockTakingSubmissionDto> UpsertDraftAsync(
        UpsertSubmissionRequest request, int distributorId, int userId, CancellationToken ct = default);

    Task<StockTakingSubmissionDto> SubmitAsync(
        int periodId, int distributorId, int userId, CancellationToken ct = default);
}
