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
        RepBillingStatus? repStatus,
        DistributorBillingStatus? distributorStatus,
        int? outletId, int? distributorId, int? salesRepId,
        DateOnly? dateFrom, DateOnly? dateTo,
        PaymentType? paymentType = null,
        bool? isCashCollected = null,
        CancellationToken ct = default);

    Task<OutletBillingSummaryResponseDto> GetOutletSummaryAsync(
        int salesRepId, int routeId,
        DateOnly dateFrom, DateOnly dateTo,
        CancellationToken ct = default);

    Task<RepMonthlySalesDto> GetRepMonthlySalesAsync(int salesRepId, int year, int month, CancellationToken ct = default);
    Task<RepDailySalesDto> GetRepDailySalesAsync(int salesRepId, DateOnly date, CancellationToken ct = default);

    Task<RepMonthlySalesItemwiseDto> GetRepMonthlySalesItemwiseAsync(
        int salesRepId, int year, int month, CancellationToken ct = default);

    Task<BillingDto> CancelAsync(int billingId, int salesRepId, CancellationToken ct = default);
    Task<BillingDto> ApproveAsync(int billingId, int userId, CancellationToken ct = default);
    Task<BillingDto> RejectAsync(int billingId, int userId, string? reason, CancellationToken ct = default);
    Task<BillingDto> UpdatePaymentTypeAsync(int billingId, int userId, PaymentType paymentType, CancellationToken ct = default);
    Task<BillingDto> UpdateCashCollectedAsync(int billingId, int userId, bool isCashCollected, CancellationToken ct = default);
}
