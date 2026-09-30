using Microsoft.Extensions.Options;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.RouteUnlockRequests.Repositories;
using sfa_api.Features.UserProximityExemptions.DTOs;
using sfa_api.Features.UserProximityExemptions.Repositories;

namespace sfa_api.Features.UserProximityExemptions.Services;

/// <summary>
/// The single place the effective geofence policy is decided. Both the mobile
/// outlet sync and the bill-create gate go through here; nothing else reads the
/// exemption or route-unlock tables for an enforcement decision.
/// </summary>
public class ProximityPolicyResolver(
    IUserProximityExemptionRepository repo,
    IRouteUnlockRequestRepository routeUnlockRepo,
    IOptions<BillingGeoOptions> geoOptions,
    ILogger<ProximityPolicyResolver> logger) : IProximityPolicyResolver
{
    private readonly IUserProximityExemptionRepository _repo = repo;
    private readonly IRouteUnlockRequestRepository _routeUnlockRepo = routeUnlockRepo;
    private readonly IOptions<BillingGeoOptions> _geoOptions = geoOptions;
    private readonly ILogger<ProximityPolicyResolver> _logger = logger;

    public async Task<ProximityPolicy> ResolveAsync(
        int userId, int? routeId = null, DateTime? atUtc = null, CancellationToken ct = default)
    {
        var geo = _geoOptions.Value;
        var at = atUtc ?? DateTime.UtcNow;

        // Config kill-switch wins and short-circuits the query. EnforcedFrom stays
        // null because nothing is scheduled to turn enforcement back on.
        if (!geo.EnforceProximity)
            return new ProximityPolicy(false, geo.RadiusMeters, geo.ToleranceMeters, null, null, null);

        // An admin grant is rep-wide, so it beats a route-scoped unlock.
        var exemption = await _repo.GetEffectiveAsync(userId, at, ct);
        if (exemption is not null)
        {
            _logger.LogInformation(
                "Proximity check relaxed for user {UserId} by exemption {ExemptionId} ({Reason}) until {ValidTo:o}",
                userId, exemption.Id, exemption.Reason, exemption.ValidTo);

            return new ProximityPolicy(
                Enforced: false,
                RadiusMeters: geo.RadiusMeters,
                ToleranceMeters: geo.ToleranceMeters,
                EnforcedFrom: exemption.ValidTo,
                ExemptionId: exemption.Id,
                Reason: exemption.Reason,
                Source: ProximityPolicySource.AdminExemption);
        }

        // A route unlock only ever applies to the route it was approved for —
        // callers that don't know the route get the enforced policy.
        if (routeId is { } rid)
        {
            var unlock = await _routeUnlockRepo.GetEffectiveAsync(userId, rid, at, ct);
            if (unlock is not null)
            {
                _logger.LogInformation(
                    "Proximity check relaxed for user {UserId} on route {RouteId} by route unlock {RequestId} until {ValidTo:o}",
                    userId, rid, unlock.Id, unlock.ValidTo);

                return new ProximityPolicy(
                    Enforced: false,
                    RadiusMeters: geo.RadiusMeters,
                    ToleranceMeters: geo.ToleranceMeters,
                    EnforcedFrom: unlock.ValidTo,
                    ExemptionId: null,
                    Reason: null,
                    Source: ProximityPolicySource.RouteUnlock,
                    RouteUnlockRequestId: unlock.Id);
            }
        }

        return new ProximityPolicy(true, geo.RadiusMeters, geo.ToleranceMeters, null, null, null);
    }
}
