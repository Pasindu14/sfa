using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using sfa_api.Features.Billings.Options;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.UserProximityExemptions.Repositories;
using sfa_api.Features.UserProximityExemptions.Services;

namespace sfa_api.UnitTests.Features.UserProximityExemptions.Services;

/// <summary>
/// The resolver is the single arbiter of whether a rep's bill must be near its
/// outlet. Both the mobile outlet sync and the bill-create gate read it, so a
/// wrong answer here either blocks legitimate billing or silently opens the
/// geofence for everyone.
/// </summary>
public class ProximityPolicyResolverTests
{
    private readonly Mock<IUserProximityExemptionRepository> _repoMock = new();

    private ProximityPolicyResolver Build(BillingGeoOptions? options = null)
        => new(
            _repoMock.Object,
            Options.Create(options ?? new BillingGeoOptions()),
            NullLogger<ProximityPolicyResolver>.Instance);

    private static UserProximityExemption Grant(
        DateTime validFrom, DateTime validTo, int id = 7) => new()
    {
        Id = id,
        UserId = 42,
        ValidFrom = validFrom,
        ValidTo = validTo,
        Reason = ProximityExemptionReason.BadOutletCoordinates,
        GrantedByUserId = 1,
        IsActive = true
    };

    [Fact]
    public async Task ResolveAsync_NoExemption_EnforcesConfiguredRadius()
    {
        _repoMock.Setup(r => r.GetEffectiveAsync(42, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserProximityExemption?)null);

        var policy = await Build().ResolveAsync(42);

        policy.Enforced.Should().BeTrue();
        policy.RadiusMeters.Should().Be(1000.0);
        policy.ToleranceMeters.Should().Be(200.0);
        policy.LimitMeters.Should().Be(1200.0);
        policy.ExemptionId.Should().BeNull();
        policy.EnforcedFrom.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_LiveExemption_RelaxesAndReportsResumption()
    {
        var now = new DateTime(2026, 9, 16, 6, 0, 0, DateTimeKind.Utc);
        var validTo = new DateTime(2026, 9, 20, 18, 30, 0, DateTimeKind.Utc);
        _repoMock.Setup(r => r.GetEffectiveAsync(42, now, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Grant(now.AddDays(-1), validTo));

        var policy = await Build().ResolveAsync(42, now);

        policy.Enforced.Should().BeFalse();
        policy.ExemptionId.Should().Be(7);
        policy.Reason.Should().Be(ProximityExemptionReason.BadOutletCoordinates);
        // Published so the device can expire the exemption without another sync.
        policy.EnforcedFrom.Should().Be(validTo);
        // The radius still travels — the app labels its picker with it.
        policy.RadiusMeters.Should().Be(1000.0);
    }

    [Fact]
    public async Task ResolveAsync_GlobalKillSwitchOff_SkipsTheExemptionLookupEntirely()
    {
        var resolver = Build(new BillingGeoOptions { EnforceProximity = false });

        var policy = await resolver.ResolveAsync(42);

        policy.Enforced.Should().BeFalse();
        // Nothing is scheduled to re-enable enforcement, so there is no resumption
        // instant to hand the client — it must not invent one and re-arm itself.
        policy.EnforcedFrom.Should().BeNull();
        _repoMock.Verify(
            r => r.GetEffectiveAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_PassesTheSuppliedInstantThrough()
    {
        // The bill-create gate and the sync must be able to ask "as of when",
        // otherwise a grant's boundaries cannot be tested at all.
        var at = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
        _repoMock.Setup(r => r.GetEffectiveAsync(42, at, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UserProximityExemption?)null);

        await Build().ResolveAsync(42, at);

        _repoMock.Verify(r => r.GetEffectiveAsync(42, at, It.IsAny<CancellationToken>()), Times.Once);
    }
}
