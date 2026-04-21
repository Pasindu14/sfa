namespace sfa_api.Features.ProductCategories.DTOs;

public record ProductCategoryDto(
    int Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProductCategoryListDto(
    IEnumerable<ProductCategoryDto> ProductCategories,
    int TotalCount,
    int Page,
    int PageSize
);
