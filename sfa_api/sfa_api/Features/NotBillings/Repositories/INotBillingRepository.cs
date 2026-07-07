using sfa_api.Features.NotBillings.DTOs;
using sfa_api.Features.NotBillings.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.Outlets.Entities;

namespace sfa_api.Features.NotBillings.Repositories;

public interface INotBillingRepository
{
    Task<long> GetNextNotBillingNumberAsync(CancellationToken ct = default);
    Task<Outlet?> GetOutletAsync(int outletId, CancellationToken ct = default);
    Task<bool> ExistsForOutletTodayAsync(int salesRepId, int outletId, DateOnly date, CancellationToken ct = default);
    Task<int?> FindIdByClientRecordIdAsync(string clientRecordId, CancellationToken ct = default);

    Task<NotBilling?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(List<NotBillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        int? outletId, int? salesRepId,
        NotBillingReason? reason,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default);

    Task AddAsync(NotBilling notBilling, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
