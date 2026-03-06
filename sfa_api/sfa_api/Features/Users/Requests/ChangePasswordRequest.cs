namespace sfa_api.Features.Users.Requests;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
