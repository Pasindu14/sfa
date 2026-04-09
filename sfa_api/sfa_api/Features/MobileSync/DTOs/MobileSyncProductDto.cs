namespace sfa_api.Features.MobileSync.DTOs;

public record MobileSyncProductDto(
    int Id,
    string Code,
    string ItemDescription,
    string? PrintDescription,
    int PiecesPerPack,
    string? ImageUrl
);

public record MobileProductListDto(
    List<MobileSyncProductDto> Products,
    int TotalCount,
    DateTime CachedAt
);
