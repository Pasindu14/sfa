namespace sfa_api.Features.Users.Requests;

public record UpdateUserRequest(
    string Name,
    string Email,
    string Phone,
    string Role,
    string? DeviceId
);
