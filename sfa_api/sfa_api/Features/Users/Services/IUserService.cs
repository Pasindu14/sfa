using sfa_api.Features.Users.DTOs;
using sfa_api.Features.Users.Requests;

namespace sfa_api.Features.Users.Services;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<UserListDto> GetAllUsersAsync(int page, int pageSize, string? search = null, string? role = null, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, int? callerId, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteUserAsync(int userId, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, int? callerId, CancellationToken ct = default);
    Task ResetPasswordAsync(int userId, ResetPasswordRequest request, int? callerId, CancellationToken ct = default);
    Task DeactivateUserAsync(int userId, int? callerId, CancellationToken ct = default);
    Task ActivateUserAsync(int userId, int? callerId, CancellationToken ct = default);
}
