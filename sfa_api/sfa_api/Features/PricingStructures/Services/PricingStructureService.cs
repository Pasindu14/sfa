using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.PricingStructures.DTOs;
using sfa_api.Features.PricingStructures.Entities;
using sfa_api.Features.PricingStructures.Repositories;
using sfa_api.Features.PricingStructures.Requests;
using sfa_api.Infrastructure.Caching;
using sfa_api.Infrastructure.Locking;

namespace sfa_api.Features.PricingStructures.Services;

public class PricingStructureService(
    IPricingStructureRepository repo,
    IDistributedLockService lockService,
    ICacheService cache,
    ILogger<PricingStructureService> logger) : IPricingStructureService
{
    private readonly IPricingStructureRepository _repo = repo;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly ICacheService _cache = cache;
    private readonly ILogger<PricingStructureService> _logger = logger;

    // Serializes every write that can move the default flag, so two admins can't race the swap.
    private const string DefaultLockKey = "pricing-structure:default";

    public async Task<PricingStructureDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        return await MapAsync(structure, ct);
    }

    public async Task<PricingStructureListDto> GetAllAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct = default)
    {
        (page, pageSize) = PaginationHelper.Clamp(page, pageSize);
        var (items, total) = await _repo.GetAllAsync((page - 1) * pageSize, pageSize, search, isActive, ct);
        return new PricingStructureListDto(items, total, page, pageSize);
    }

    public async Task<PricingStructureDto> CreateAsync(CreatePricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        await using var @lock = await AcquireDefaultLockAsync(ct);

        var name = request.Name.Trim();
        if (await _repo.NameExistsAsync(name, null, ct))
            throw new DuplicateResourceException("Name");

        // The very first structure becomes the (active) default — the system must always have one.
        // Every later structure starts inactive so reps never sync a half-priced list.
        var isFirst = await _repo.GetDefaultIdAsync(ct) is null;

        var structure = new PricingStructure
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsDefault = isFirst,
            IsActive = isFirst,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(structure, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} '{Name}' created by {CallerId}", structure.Id, structure.Name, callerId);
        await InvalidateAsync(ct);
        return await MapAsync(structure, ct);
    }

    public async Task<PricingStructureDto> UpdateAsync(int id, UpdatePricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        var name = request.Name.Trim();
        if (await _repo.NameExistsAsync(name, id, ct))
            throw new DuplicateResourceException("Name");

        _repo.ApplyConcurrencyToken(structure, request.RowVersion);
        structure.Name = name;
        structure.Description = request.Description?.Trim();
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} updated by {CallerId}", id, callerId);
        await InvalidateAsync(ct);
        return await MapAsync(structure, ct);
    }

    public async Task<PricingStructureDto> DuplicateAsync(int sourceId, DuplicatePricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        var source = await _repo.GetByIdAsync(sourceId, ct)
            ?? throw new NotFoundException("PricingStructure", sourceId);

        var name = request.Name.Trim();
        if (await _repo.NameExistsAsync(name, null, ct))
            throw new DuplicateResourceException("Name");

        var sourceItems = await _repo.GetItemsAsync(source.Id, ct);
        var now = DateTime.UtcNow;
        PricingStructure copy = null!;

        // Header + every item in one transaction: a half-copied structure must never exist.
        var strategy = _repo.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _repo.BeginTransactionAsync(ct);
            copy = new PricingStructure
            {
                Name = name,
                Description = request.Description?.Trim(),
                IsDefault = false,
                IsActive = false,
                CreatedBy = callerId,
                UpdatedBy = callerId,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _repo.AddAsync(copy, ct);
            await _repo.SaveChangesAsync(ct);

            await _repo.AddItemsAsync(sourceItems.Select(i => new PricingStructureItem
            {
                PricingStructureId = copy.Id,
                ProductId = i.ProductId,
                DealerPackPrice = i.DealerPackPrice,
                DealerCasePrice = i.DealerCasePrice,
                Mrp = i.Mrp,
                CreatedBy = callerId,
                UpdatedBy = callerId,
                CreatedAt = now,
                UpdatedAt = now
            }), ct);
            await _repo.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _logger.LogInformation(
            "PricingStructure {SourceId} duplicated as {PricingStructureId} '{Name}' ({ItemCount} items) by {CallerId}",
            sourceId, copy.Id, copy.Name, sourceItems.Count, callerId);
        await InvalidateAsync(ct);
        return await MapAsync(copy, ct);
    }

    public async Task<PricingStructureDto> SetDefaultAsync(int id, SetDefaultPricingStructureRequest request, int? callerId, CancellationToken ct = default)
    {
        await using var @lock = await AcquireDefaultLockAsync(ct);

        var target = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        if (!target.IsActive)
            throw new BusinessRuleException("PRICING_STRUCTURE_INACTIVE",
                "Only an active pricing structure can be the default. Activate it first.");

        if (target.IsDefault)
            return await MapAsync(target, ct);

        _repo.ApplyConcurrencyToken(target, request.RowVersion);
        var current = await _repo.GetDefaultAsync(ct);
        var now = DateTime.UtcNow;

        // Clear the old default before setting the new one — the partial unique index allows at most
        // one live default at any instant, including mid-transaction.
        var strategy = _repo.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _repo.BeginTransactionAsync(ct);
            if (current is not null)
            {
                current.IsDefault = false;
                current.UpdatedBy = callerId;
                current.UpdatedAt = now;
                await _repo.SaveChangesAsync(ct);
            }
            target.IsDefault = true;
            target.UpdatedBy = callerId;
            target.UpdatedAt = now;
            await _repo.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        _logger.LogInformation("PricingStructure default changed {OldId} -> {NewId} by {CallerId}", current?.Id, id, callerId);
        await InvalidateAsync(ct);
        return await MapAsync(target, ct);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        if (structure.IsActive) return;

        structure.IsActive = true;
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} activated by {CallerId}", id, callerId);
        await InvalidateAsync(ct);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        await using var @lock = await AcquireDefaultLockAsync(ct);

        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        EnsureNotDefault(structure, "deactivated");
        if (!structure.IsActive) return;

        structure.IsActive = false;
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} deactivated by {CallerId}", id, callerId);
        await InvalidateAsync(ct);
    }

    public async Task DeleteAsync(int id, int? callerId, CancellationToken ct = default)
    {
        await using var @lock = await AcquireDefaultLockAsync(ct);

        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        EnsureNotDefault(structure, "deleted");

        // Soft delete. Bills keep their FK and still resolve the name; the items stay for history.
        structure.IsActive = false;
        structure.IsDeleted = true;
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("PricingStructure {PricingStructureId} deleted by {CallerId}", id, callerId);
        await InvalidateAsync(ct);
    }

    public async Task<IReadOnlyList<PricingStructureItemRowDto>> GetItemsAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);
        return await _repo.GetItemRowsAsync(id, ct);
    }

    public async Task<PricingStructureDto> UpsertItemsAsync(int id, BulkUpsertPricingStructureItemsRequest request, int? callerId, CancellationToken ct = default)
    {
        var structure = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("PricingStructure", id);

        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var known = await _repo.GetExistingProductIdsAsync(productIds, ct);
        var missing = productIds.FirstOrDefault(p => !known.Contains(p));
        if (missing != 0)
            throw new NotFoundException("Product", missing);

        var existing = (await _repo.GetItemsForProductsAsync(id, productIds, ct))
            .ToDictionary(i => i.ProductId);
        var now = DateTime.UtcNow;
        var added = new List<PricingStructureItem>();

        foreach (var row in request.Items)
        {
            if (existing.TryGetValue(row.ProductId, out var item))
            {
                // Rows are never removed — clearing every price leaves an unpriced row behind.
                item.DealerPackPrice = row.DealerPackPrice;
                item.DealerCasePrice = row.DealerCasePrice;
                item.Mrp = row.Mrp;
                item.UpdatedBy = callerId;
                item.UpdatedAt = now;
            }
            else if (row.DealerPackPrice.HasValue)
            {
                added.Add(new PricingStructureItem
                {
                    PricingStructureId = id,
                    ProductId = row.ProductId,
                    DealerPackPrice = row.DealerPackPrice,
                    DealerCasePrice = row.DealerCasePrice,
                    Mrp = row.Mrp,
                    CreatedBy = callerId,
                    UpdatedBy = callerId,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        if (added.Count > 0)
            await _repo.AddItemsAsync(added, ct);
        structure.UpdatedBy = callerId;
        structure.UpdatedAt = now;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "PricingStructure {PricingStructureId} prices saved: {Updated} updated, {Added} added, by {CallerId}",
            id, existing.Count, added.Count, callerId);
        await InvalidateAsync(ct);
        return await MapAsync(structure, ct);
    }

    public async Task<DefaultPricingStructurePricesDto> GetDefaultPricesAsync(CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync<DefaultPricingStructurePricesDto>(PricingStructureCacheKeys.DefaultPrices, ct);
        if (cached is not null) return cached;

        var result = await _repo.GetDefaultPricesAsync(ct)
            ?? throw new NotFoundException("PricingStructure", "default");
        await _cache.SetAsync(PricingStructureCacheKeys.DefaultPrices, result, PricingStructureCacheKeys.DefaultPricesTtl, ct);
        return result;
    }

    private static void EnsureNotDefault(PricingStructure structure, string action)
    {
        if (structure.IsDefault)
            throw new BusinessRuleException("PRICING_STRUCTURE_IS_DEFAULT",
                $"The default pricing structure cannot be {action}. Set another structure as the default first.");
    }

    private async Task<IAsyncDisposable> AcquireDefaultLockAsync(CancellationToken ct)
        => await _lockService.AcquireAsync(DefaultLockKey, ct)
           ?? throw new ConcurrencyConflictException(new { message = "Another pricing structure change is in progress. Try again." });

    private async Task InvalidateAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(PricingStructureCacheKeys.DefaultPrices, ct);
        await _cache.RemoveAsync(PricingStructureCacheKeys.MobileSync, ct);
        await _cache.RemoveAsync(PricingStructureCacheKeys.MobileProducts, ct);   // legacy price fields come from the default
    }

    private async Task<PricingStructureDto> MapAsync(PricingStructure s, CancellationToken ct) => new(
        s.Id, s.Name, s.Description, s.IsDefault, s.IsActive,
        await _repo.GetPricedCountAsync(s.Id, ct),
        s.RowVersion, s.CreatedAt, s.UpdatedAt);
}
