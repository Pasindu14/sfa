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
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Slim row for product pickers — only what a dropdown shows.</summary>
public record ProductLookupDto(int Id, string Code, string ItemDescription);

public record ProductListDto(
    IEnumerable<ProductDto> Products,
    int TotalCount,
    int Page,
    int PageSize
);
