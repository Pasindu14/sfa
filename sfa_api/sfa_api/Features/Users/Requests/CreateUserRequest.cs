namespace sfa_api.Features.Users.Requests;

public record CreateUserRequest(
    string Name,
    string Username,
    string Email,
    string Phone,
    string Password,
    string Role,
    string? DeviceId
);
