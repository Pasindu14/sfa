using sfa_api.Features.UserProximityExemptions.DTOs;

namespace sfa_api.Features.UserProximityExemptions.Services;

public interface IProximityPolicyResolver
{
    /// <summary>
    /// Resolves the effective geofence policy for <paramref name="userId"/> at
    /// <paramref name="atUtc"/> (defaults to now). Falls back to the configured
    /// <c>BillingGeo</c> options when the rep holds no live exemption.
    /// </summary>
    Task<ProximityPolicy> ResolveAsync(int userId, DateTime? atUtc = null, CancellationToken ct = default);
}
