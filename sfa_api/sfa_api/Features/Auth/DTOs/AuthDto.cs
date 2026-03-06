namespace sfa_api.Features.Auth.DTOs;

// Returned on successful login or refresh
public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    DateTime RefreshTokenExpiry,
    UserProfileDto User
);

// Basic user profile returned with token
public record UserProfileDto(
    int Id,
    string Name,
    string Email,
    string Role
);
