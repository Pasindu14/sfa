using Microsoft.Extensions.Options;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.UserProximityExemptions.DTOs;
using sfa_api.Features.UserProximityExemptions.Repositories;

namespace sfa_api.Features.UserProximityExemptions.Services;

/// <summary>
/// The single place the effective geofence policy is decided. Both the mobile
/// outlet sync and the bill-create gate go through here; nothing else reads the
/// exemption table.
/// </summary>
public class ProximityPolicyResolver(
    IUserProximityExemptionRepository repo,
    IOptions<BillingGeoOptions> geoOptions,
    ILogger<ProximityPolicyResolver> logger) : IProximityPolicyResolver
{
    private readonly IUserProximityExemptionRepository _repo = repo;
    private readonly IOptions<BillingGeoOptions> _geoOptions = geoOptions;
    private readonly ILogger<ProximityPolicyResolver> _logger = logger;

    public async Task<ProximityPolicy> ResolveAsync(
        int userId, DateTime? atUtc = null, CancellationToken ct = default)
    {
        var geo = _geoOptions.Value;
        var at = atUtc ?? DateTime.UtcNow;

        // Config kill-switch wins and short-circuits the query. EnforcedFrom stays
        // null because nothing is scheduled to turn enforcement back on.
        if (!geo.EnforceProximity)
            return new ProximityPolicy(false, geo.RadiusMeters, geo.ToleranceMeters, null, null, null);

        var exemption = await _repo.GetEffectiveAsync(userId, at, ct);
        if (exemption is null)
            return new ProximityPolicy(true, geo.RadiusMeters, geo.ToleranceMeters, null, null, null);

        _logger.LogInformation(
            "Proximity check relaxed for user {UserId} by exemption {ExemptionId} ({Reason}) until {ValidTo:o}",
            userId, exemption.Id, exemption.Reason, exemption.ValidTo);

        return new ProximityPolicy(
            Enforced: false,
            RadiusMeters: geo.RadiusMeters,
            ToleranceMeters: geo.ToleranceMeters,
            EnforcedFrom: exemption.ValidTo,
            ExemptionId: exemption.Id,
            Reason: exemption.Reason);
    }
}
