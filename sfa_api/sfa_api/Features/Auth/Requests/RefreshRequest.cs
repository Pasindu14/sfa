namespace sfa_api.Features.Auth.Requests;

public record RefreshRequest(
    string RefreshToken,
    string DeviceId
);
