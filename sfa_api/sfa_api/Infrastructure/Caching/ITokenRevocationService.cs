namespace sfa_api.Infrastructure.Caching;

public interface ITokenRevocationService
{
    Task RevokeAsync(string jti, DateTime tokenExpiry, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(string jti, CancellationToken ct = default);
}
