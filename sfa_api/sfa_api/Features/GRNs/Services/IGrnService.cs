using sfa_api.Features.GRNs.DTOs;
using sfa_api.Features.GRNs.Requests;

namespace sfa_api.Features.GRNs.Services;

public interface IGrnService
{
    Task<(List<GrnDto> Items, int TotalCount)> GetListAsync(int page, int pageSize, string? status, int? distributorId, DateOnly? dateFrom = null, DateOnly? dateTo = null, string? search = null, CancellationToken ct = default);
    Task<GrnDto> GetByIdAsync(int grnId, CancellationToken ct = default);
    Task<GrnDto> CreateAsync(CreateGrnRequest request, int callerId, CancellationToken ct = default);
    Task<GrnDto> ConfirmAsync(int grnId, ConfirmGrnRequest request, int callerId, CancellationToken ct = default);
}
