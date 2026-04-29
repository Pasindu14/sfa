using sfa_api.Features.Billings.DTOs;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Billings.Requests;

namespace sfa_api.Features.Billings.Services;

public interface IBillingService
{
    Task<BillingDto> CreateAsync(CreateBillingRequest request, int salesRepId, CancellationToken ct = default);
    Task<BillingDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<BillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        BillingStatus? status,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default);

    Task<OutletBillingSummaryResponseDto> GetOutletSummaryAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default);
}
