using sfa_api.Features.NotBillings.DTOs;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.NotBillings.Requests;

namespace sfa_api.Features.NotBillings.Services;

public interface INotBillingService
{
    Task<NotBillingDto> CreateAsync(CreateNotBillingRequest request, int salesRepId, CancellationToken ct = default);
    Task<NotBillingDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<NotBillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        int? outletId, int? salesRepId,
        NotBillingReason? reason,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default);
}
