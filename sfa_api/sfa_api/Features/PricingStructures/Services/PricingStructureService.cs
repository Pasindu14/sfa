using sfa_api.Common.Errors;
using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Features.Products.Repositories;

namespace sfa_api.Features.PricingStructures.Services;

public class PricingStructureService(
    IPricingStructureRepository repo,
    IProductRepository productRepo,
    ILogger<PricingStructureService> logger) : IPricingStructureService
{
    private readonly IPricingStructureRepository _repo = repo;
    private readonly IProductRepository _productRepo = productRepo;
    private readonly ILogger<PricingStructureService> _logger = logger;

    public async Task<PricingStructureDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        return MapToDetailDto(structure);
    }

    public async Task<PricingStructureDetailDto> GetDefaultAsync(CancellationToken ct = default)
    {
        var structure = await _repo.GetCurrentDefaultAsync(ct)
            ?? throw new NotFoundException("PricingStructure", "default");
        var withItems = await _repo.GetByIdWithItemsAsync(structure.Id, ct)
            ?? throw new NotFoundException("PricingStructure", structure.Id);
        return MapToDetailDto(withItems);
    }

    public async Task<PricingStructureListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (structures, totalCount) = await _repo.GetAllAsync(skip, pageSize, search, ct);
        return new PricingStructureListDto(
            PricingStructures: structures.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<PricingStructureDto> CreateAsync(CreatePricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByNameAsync(request.Name, ct);
        if (existing != null)
            throw new DuplicateResourceException("Name");

        if (request.IsDefault)
        {
            var currentDefault = await _repo.GetCurrentDefaultAsync(ct);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
                currentDefault.UpdatedBy = callerId;
                currentDefault.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(currentDefault, ct);
            }
        }

        var structure = new PricingStructure
        {
            Name = request.Name,
            Description = request.Description,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(structure, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} created with name {Name}", structure.Id, structure.Name);
        return MapToDto(structure);
    }

    public async Task<PricingStructureDto> UpdateAsync(int id, UpdatePricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        var duplicate = await _repo.GetByNameAsync(request.Name, ct);
        if (duplicate != null && duplicate.Id != id)
            throw new DuplicateResourceException("Name");

        if (request.IsDefault && !structure.IsDefault)
        {
            var currentDefault = await _repo.GetCurrentDefaultAsync(ct);
            if (currentDefault != null && currentDefault.Id != id)
            {
                currentDefault.IsDefault = false;
                currentDefault.UpdatedBy = callerId;
                currentDefault.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(currentDefault, ct);
            }
        }

        structure.Name = request.Name;
        structure.Description = request.Description;
        structure.IsDefault = request.IsDefault;
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(structure, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} updated", id);
        return MapToDto(structure);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        await _repo.DeactivateAsync(id, ct);
        _logger.LogInformation("PricingStructure {PricingStructureId} deactivated", id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        await _repo.DeleteAsync(id, ct);
        _logger.LogInformation("PricingStructure {PricingStructureId} deleted", id);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        await _repo.ActivateAsync(id, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} activated", id);
    }

    public async Task<IEnumerable<PricingStructureItemDto>> GetItemsAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        var items = await _repo.GetItemsAsync(id, ct);
        return items.Select(MapItemToDto);
    }

    public async Task<IEnumerable<PricingStructureItemDto>> BulkReplaceItemsAsync(int id, BulkUpdateItemsRequest request, int? callerId, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var (activeProducts, _) = await _productRepo.GetAllAsync(0, int.MaxValue, null, ct);
        var activeProductIds = activeProducts.Select(p => p.Id).ToHashSet();

        var invalidIds = productIds.Where(pid => !activeProductIds.Contains(pid)).ToList();
        if (invalidIds.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Items", new[] { $"The following product IDs do not exist or are not active: {string.Join(", ", invalidIds)}" } }
            });

        var newItems = request.Items.Select(i => new PricingStructureItem
        {
            PricingStructureId = id,
            ProductId = i.ProductId,
            DealerPackPrice = i.DealerPackPrice,
            DealerCasePrice = i.DealerCasePrice,
            PromotionalPrice = i.PromotionalPrice,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }).ToList();

        await _repo.BulkReplaceItemsAsync(id, newItems, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} items bulk replaced with {Count} items", id, newItems.Count);

        var updatedItems = await _repo.GetItemsAsync(id, ct);
        return updatedItems.Select(MapItemToDto);
    }

    private static PricingStructureDto MapToDto(PricingStructure structure) => new(
        Id: structure.Id,
        Name: structure.Name,
        Description: structure.Description,
        IsDefault: structure.IsDefault,
        IsActive: structure.IsActive,
        ItemCount: structure.Items.Count,
        CreatedAt: structure.CreatedAt,
        UpdatedAt: structure.UpdatedAt
    );

    private static PricingStructureDetailDto MapToDetailDto(PricingStructure structure) => new(
        Id: structure.Id,
        Name: structure.Name,
        Description: structure.Description,
        IsDefault: structure.IsDefault,
        IsActive: structure.IsActive,
        ItemCount: structure.Items.Count,
        CreatedAt: structure.CreatedAt,
        UpdatedAt: structure.UpdatedAt,
        Items: structure.Items.Select(MapItemToDto)
    );

    private static PricingStructureItemDto MapItemToDto(PricingStructureItem item) => new(
        Id: item.Id,
        PricingStructureId: item.PricingStructureId,
        ProductId: item.ProductId,
        ProductCode: item.Product.Code,
        ProductItemDescription: item.Product.ItemDescription,
        DealerPackPrice: item.DealerPackPrice,
        DealerCasePrice: item.DealerCasePrice,
        PromotionalPrice: item.PromotionalPrice
    );
}
