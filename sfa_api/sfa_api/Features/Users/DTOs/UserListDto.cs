namespace sfa_api.Features.Users.DTOs;

public record UserListDto(
    IEnumerable<UserDto> Users,
    int TotalCount,
    int Page,
    int PageSize
);
