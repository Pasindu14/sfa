namespace sfa_api.Features.Auth.Requests;

public record DevTokenRequest(
    int UserId,
    int? ExpiryDays   // Defaults to 365 if not provided; max 3650 (10 years)
);
