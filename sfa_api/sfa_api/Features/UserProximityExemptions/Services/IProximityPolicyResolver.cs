using sfa_api.Features.UserProximityExemptions.DTOs;

namespace sfa_api.Features.UserProximityExemptions.Services;

public interface IProximityPolicyResolver
{
    /// <summary>
    /// Resolves the effective geofence policy for <paramref name="userId"/> at
    /// <paramref name="atUtc"/> (defaults to now). An admin exemption applies on
    /// any route; an approved route unlock applies only when
    /// <paramref name="routeId"/> is the route it was approved for. Falls back to
    /// the configured <c>BillingGeo</c> options when neither is live.
    /// </summary>
    Task<ProximityPolicy> ResolveAsync(
        int userId, int? routeId = null, DateTime? atUtc = null, CancellationToken ct = default);
}
