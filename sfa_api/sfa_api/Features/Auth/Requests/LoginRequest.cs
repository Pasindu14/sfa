namespace sfa_api.Features.Auth.Requests;

public record LoginRequest(
    string Username,
    string Password,
    string? DeviceId    // Optional - required only for Sales Reps
);
