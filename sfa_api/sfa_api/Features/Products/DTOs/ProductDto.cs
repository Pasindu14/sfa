namespace sfa_api.Features.Products.DTOs;

public record ProductDto(
    int Id,
    string Code,
    string ItemDescription,
    string? PrintDescription,
    int PiecesPerPack,
    string? ImageUrl,
    string? Remarks,
    int? FleetId,
    string? FleetName,
    int? CategoryId,
    string? CategoryName,
    bool IsActive,
    decimal DealerPackPrice,
    decimal DealerCasePrice,
    decimal Mrp,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProductListDto(
    IEnumerable<ProductDto> Products,
    int TotalCount,
    int Page,
    int PageSize
);
