using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Requests;

namespace sfa_api.Features.PricingStructures.Services;

public interface IPricingStructureService
{
    Task<PricingStructureDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PricingStructureDetailDto> GetDefaultAsync(CancellationToken ct = default);
    Task<PricingStructureListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<PricingStructureDto> CreateAsync(CreatePricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task<PricingStructureDto> UpdateAsync(int id, UpdatePricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task ActivateAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<PricingStructureItemDto>> GetItemsAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<PricingStructureItemDto>> BulkReplaceItemsAsync(int id, BulkUpdateItemsRequest request, int? callerId, CancellationToken ct = default);
}
