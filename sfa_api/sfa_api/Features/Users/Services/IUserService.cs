using sfa_api.Features.Users.DTOs;
using sfa_api.Features.Users.Requests;

namespace sfa_api.Features.Users.Services;

public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int userId, CancellationToken ct = default);
    Task<UserListDto> GetAllUsersAsync(int page, int pageSize, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, CancellationToken ct = default);
    Task DeleteUserAsync(int userId, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(int userId, ResetPasswordRequest request, CancellationToken ct = default);
    Task DeactivateUserAsync(int userId, CancellationToken ct = default);
}
