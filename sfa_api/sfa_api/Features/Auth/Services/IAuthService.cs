using sfa_api.Features.Auth.DTOs;
using sfa_api.Features.Auth.Requests;

namespace sfa_api.Features.Auth.Services;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAllAsync(int userId, CancellationToken ct = default);
}
