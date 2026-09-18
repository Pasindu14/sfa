namespace sfa_api.Features.PricingStructures.Requests;

public record CreatePricingStructureRequest(string Name, string? Description);

public record UpdatePricingStructureRequest(string Name, string? Description, uint RowVersion);

/// <summary>Copies the source structure and all its product prices into a new, inactive structure.</summary>
public record DuplicatePricingStructureRequest(string Name, string? Description);

public record SetDefaultPricingStructureRequest(uint RowVersion);

/// <summary>A changed grid row. Null prices clear that price (the product becomes unpriced when pack is null).</summary>
public record PricingStructureItemUpsert(
    int ProductId,
    decimal? DealerPackPrice,
    decimal? DealerCasePrice,
    decimal? Mrp
);

public record BulkUpsertPricingStructureItemsRequest(List<PricingStructureItemUpsert> Items);
