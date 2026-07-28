namespace sfa_api.Features.Users.DTOs;

public record UserDto(
    int Id,
    string Name,
    string Username,
    string Email,
    string Phone,
    string Role,
    int? DistributorId,
    string? DistributorName,
    string? DeviceId,
    bool IsActive,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
