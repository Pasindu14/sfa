using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Requests;

namespace sfa_api.Features.PricingStructures.Services;

public interface IPricingStructureService
{
    Task<PricingStructureDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PricingStructureListDto> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default);
    Task<PricingStructureDto> CreateAsync(CreatePricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task<PricingStructureDto> UpdateAsync(int id, UpdatePricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task<PricingStructureDto> DuplicateAsync(int sourceId, DuplicatePricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task<PricingStructureDto> SetDefaultAsync(int id, SetDefaultPricingStructureRequest request, int? callerId, CancellationToken ct = default);
    Task ActivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, int? callerId, CancellationToken ct = default);
    Task<IReadOnlyList<PricingStructureItemRowDto>> GetItemsAsync(int id, CancellationToken ct = default);
    Task<PricingStructureDto> UpsertItemsAsync(int id, BulkUpsertPricingStructureItemsRequest request, int? callerId, CancellationToken ct = default);
    Task<DefaultPricingStructurePricesDto> GetDefaultPricesAsync(CancellationToken ct = default);
}
