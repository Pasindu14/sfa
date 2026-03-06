namespace sfa_api.Features.Users.Requests;

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
