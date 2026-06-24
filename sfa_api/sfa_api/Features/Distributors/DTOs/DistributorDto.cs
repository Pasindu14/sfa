namespace sfa_api.Features.Distributors.DTOs;

public record DistributorDto(
    int Id,
    string Name,
    string Address,
    string Phone,
    string Email,
    int Alias,
    decimal TradeDiscount,
    decimal Commission,
    string Category,
    string? Remark,
    string? VatRegNo,
    double? Latitude,
    double? Longitude,
    int? TerritoryId,
    string? TerritoryName,
    int? AreaId,
    int? RegionId,
    int? FleetId,
    string? FleetName,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record DistributorListDto(
    IEnumerable<DistributorDto> Distributors,
    int TotalCount,
    int Page,
    int PageSize
);
