namespace sfa_api.Features.Users.DTOs;

public record UserDto(
    int Id,
    string Name,
    string Username,
    string Email,
    string Phone,
    string Role,
    bool IsActive,
    int? CreatedById,
    int? UpdatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
