using sfa_api.Features.Distributors.DTOs;
using sfa_api.Features.Distributors.Requests;

namespace sfa_api.Features.Distributors.Services;

public interface IDistributorService
{
    Task<DistributorDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<DistributorListDto> GetAllAsync(int page, int pageSize, string? search = null, bool? isActive = null, CancellationToken ct = default);
    Task<DistributorDto> CreateAsync(CreateDistributorRequest request, int? callerId, CancellationToken ct = default);
    Task<DistributorDto> UpdateAsync(int id, UpdateDistributorRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
}
