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
    string? Remark,
    string? VatRegNo,
    double? Latitude,
    double? Longitude,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record DistributorListDto(
    IEnumerable<DistributorDto> Distributors,
    int TotalCount,
    int Page,
    int PageSize
);
