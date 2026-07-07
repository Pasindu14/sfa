using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Auth.DTOs;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Auth.Repositories;
using sfa_api.Features.Auth.Requests;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.Features.Auth.Services;

public class AuthService(
    IAuthRepository repo,
    IJwtTokenHelper jwtHelper,
    ITokenRevocationService revocationService,
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly Dictionary<string, string[]> DeviceIdRequiredError = new()
    {
        { "DeviceId", new[] { "Device ID is required for Sales Reps." } }
    };

    private static readonly Dictionary<string, string[]> DeviceMismatchError = new()
    {
        { "DeviceId", new[] { "This device is not registered for your account. Contact your administrator." } }
    };

    private readonly IAuthRepository _repo = repo;
    private readonly IJwtTokenHelper _jwtHelper = jwtHelper;
    private readonly ITokenRevocationService _revocationService = revocationService;
    private readonly IConfiguration _config = config;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        // 1. Find user by username
        var user = await _repo.GetUserByUsernameAsync(request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Generic message — never reveal if username exists or not
            throw new AuthenticationException(
                "AUTH_INVALID_CREDENTIALS",
                "Invalid username or password.");
        }

        // 2. Check account is active
        if (!user.IsActive)
            throw new AuthenticationException("AUTH_ACCOUNT_DISABLED", "Account is disabled.");

        // 3. Device binding — TOFU (Trust On First Use) for Sales Reps
        if (user.Role == UserRole.SalesRep)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
                throw new ValidationException(DeviceIdRequiredError);

            if (string.IsNullOrEmpty(user.DeviceId))
            {
                // First login from this device — register it
                await _repo.RegisterDeviceAsync(user.Id, request.DeviceId, ct);
                _logger.LogInformation(
                    "Device registered for user {UserId}: {DeviceId}",
                    user.Id, request.DeviceId);
            }
            else if (user.DeviceId != request.DeviceId)
            {
                // Known device mismatch — wrong device
                throw new AuthenticationException("AUTH_DEVICE_MISMATCH",
                    "This device is not registered for your account.");
            }
        }

        // 4. Generate access token
        var accessToken = _jwtHelper.GenerateAccessToken(user, out _);

        // 4. Generate and store refresh token
        var plainRefreshToken = _jwtHelper.GenerateRefreshToken();
        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays");

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtHelper.HashToken(plainRefreshToken),
            FamilyId = Guid.NewGuid(),      // New family for each login session
            DeviceId = request.DeviceId ?? string.Empty,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays)
        };

        await _repo.AddRefreshTokenAsync(refreshToken, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} logged in from device {DeviceId}",
            user.Id, request.DeviceId ?? string.Empty);

        return BuildAuthResponse(accessToken, plainRefreshToken, refreshToken, user);
    }

    public async Task<AuthResponseDto> RefreshAsync(
        RefreshRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwtHelper.HashToken(request.RefreshToken);
        var storedToken = await _repo.GetRefreshTokenByHashAsync(tokenHash, ct)
            ?? throw new InvalidTokenException();

        // Reuse detected — potential theft, revoke entire family
        if (storedToken.IsConsumed)
        {
            _logger.LogCritical(
                "Refresh token reuse detected. UserId: {UserId} FamilyId: {FamilyId}. " +
                "Revoking entire token family.",
                storedToken.UserId, storedToken.FamilyId);

            await _repo.RevokeTokenFamilyAsync(storedToken.FamilyId, ct);
            await _repo.SaveChangesAsync(ct);
            throw new InvalidTokenException();
        }

        if (storedToken.IsRevoked)
            throw new InvalidTokenException();

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new TokenExpiredException();

        if (storedToken.DeviceId != request.DeviceId)
            throw new InvalidTokenException();

        // Consume old token
        storedToken.IsConsumed = true;

        // Guard: user may have been deactivated or deleted since token was issued
        var user = storedToken.User
            ?? throw new InvalidTokenException();

        // A deactivated user (IsActive == false — e.g. a terminated rep) must not be able to
        // mint fresh access tokens via refresh (finding #9). Revoke the whole family so every
        // outstanding refresh token for this session dies too, then reject.
        if (!user.IsActive)
        {
            await _repo.RevokeTokenFamilyAsync(storedToken.FamilyId, ct);
            await _repo.SaveChangesAsync(ct);
            _logger.LogWarning(
                "Refresh rejected for deactivated user {UserId}; token family {FamilyId} revoked.",
                user.Id, storedToken.FamilyId);
            throw new InvalidTokenException();
        }

        // Issue new access token
        var newAccessToken = _jwtHelper.GenerateAccessToken(user, out _);

        // Issue new refresh token — same family, rotated
        var newPlainRefreshToken = _jwtHelper.GenerateRefreshToken();
        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtHelper.HashToken(newPlainRefreshToken),
            FamilyId = storedToken.FamilyId,    // Same family continues
            DeviceId = request.DeviceId,
            ExpiresAt = storedToken.ExpiresAt   // Keep original session expiry
        };

        await _repo.AddRefreshTokenAsync(newRefreshToken, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);

        return BuildAuthResponse(newAccessToken, newPlainRefreshToken, newRefreshToken, user);
    }

    public async Task LogoutAsync(string refreshToken, string? accessTokenJti = null,
        DateTime? accessTokenExpiresAt = null, CancellationToken ct = default)
    {
        // Revoke the caller's current access token so it cannot be used after logout,
        // even though it is still cryptographically valid and unexpired.
        await RevokeAccessTokenAsync(accessTokenJti, accessTokenExpiresAt, ct);

        var tokenHash = _jwtHelper.HashToken(refreshToken);
        var storedToken = await _repo.GetRefreshTokenByHashAsync(tokenHash, ct);

        if (storedToken is null) return; // Already gone — idempotent

        await _repo.RevokeTokenFamilyAsync(storedToken.FamilyId, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} logged out from device {DeviceId}",
            storedToken.UserId, storedToken.DeviceId);
    }

    public async Task LogoutAllAsync(int userId, string? accessTokenJti = null,
        DateTime? accessTokenExpiresAt = null, CancellationToken ct = default)
    {
        // Kill all refresh tokens (no new access tokens can be minted) and revoke the
        // caller's current access token immediately.
        await RevokeAccessTokenAsync(accessTokenJti, accessTokenExpiresAt, ct);

        await _repo.RevokeAllUserTokensAsync(userId, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged out from all devices", userId);
    }

    private async Task RevokeAccessTokenAsync(string? jti, DateTime? expiresAt, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(jti) || !expiresAt.HasValue || expiresAt.Value <= DateTime.UtcNow)
            return;

        await _revocationService.RevokeAsync(jti, expiresAt.Value, ct);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private AuthResponseDto BuildAuthResponse(
        string accessToken,
        string plainRefreshToken,
        RefreshToken refreshToken,
        User user)
    {
        var accessExpiry = DateTime.UtcNow.AddMinutes(
            _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes"));

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: plainRefreshToken,
            AccessTokenExpiry: accessExpiry,
            RefreshTokenExpiry: refreshToken.ExpiresAt,
            User: new UserProfileDto(
                Id: user.Id,
                Name: user.Name,
                Email: user.Email,
                Role: user.Role.ToString()
            )
        );
    }
}
