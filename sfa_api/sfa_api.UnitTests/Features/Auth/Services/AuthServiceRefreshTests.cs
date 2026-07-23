using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Auth.Entities;
using sfa_api.Features.Auth.Repositories;
using sfa_api.Features.Auth.Requests;
using sfa_api.Features.Auth.Services;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.Auth.Services;

/// <summary>
/// Covers the reuse-vs-race branch in <see cref="AuthService.RefreshAsync"/>.
///
/// Refresh tokens rotate on every use, so two callers holding the same token race: the first
/// consumes it, the second arrives with a value already marked consumed. Treating that as
/// theft revoked the whole family and signed users out mid-session — most visibly when a user
/// returned to an idle tab and the session refetch and their first click refreshed at once.
/// These tests pin both halves of the distinction: a race must survive, genuine reuse must
/// still kill the family.
/// </summary>
public class AuthServiceRefreshTests
{
    private const string DeviceId = "device-001";
    private const string PlainToken = "plain-refresh-token";
    private const string TokenHash = "hashed-refresh-token";

    private readonly Mock<IAuthRepository> _repoMock;
    private readonly Mock<IJwtTokenHelper> _jwtMock;
    private readonly Mock<ITokenRevocationService> _revocationMock;

    public AuthServiceRefreshTests()
    {
        _repoMock       = new Mock<IAuthRepository>();
        _jwtMock        = new Mock<IJwtTokenHelper>();
        _revocationMock = new Mock<ITokenRevocationService>();

        var jti = "jti-1";
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), out jti)).Returns("access-token");
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _jwtMock.Setup(j => j.HashToken(It.IsAny<string>())).Returns(TokenHash);
    }

    // GetValue<T> is an extension method and cannot be mocked — build a real configuration.
    private AuthService CreateSut(int graceSeconds = 60) => new(
        _repoMock.Object,
        _jwtMock.Object,
        _revocationMock.Object,
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpiryMinutes"]  = "30",
                ["Jwt:RefreshTokenExpiryDays"]    = "7",
                ["Jwt:RefreshReuseGraceSeconds"]  = graceSeconds.ToString()
            })
            .Build(),
        NullLogger<AuthService>.Instance);

    private static User CreateFakeUser(bool isActive = true) => new()
    {
        Id       = 7,
        Name     = "Test Rep",
        Username = "testrep",
        Email    = "rep@example.com",
        Role     = UserRole.Admin,
        IsActive = isActive
    };

    private static RefreshToken CreateStoredToken(
        Guid familyId,
        bool isConsumed = false,
        bool isRevoked  = false,
        bool isActive   = true) => new()
    {
        UserId    = 7,
        TokenHash = TokenHash,
        FamilyId  = familyId,
        DeviceId  = DeviceId,
        IsConsumed = isConsumed,
        IsRevoked  = isRevoked,
        ExpiresAt  = DateTime.UtcNow.AddDays(7),
        CreatedAt  = DateTime.UtcNow.AddMinutes(-30),
        User       = CreateFakeUser(isActive)
    };

    /// <summary>The successor token a concurrent caller already minted, created <paramref name="secondsAgo"/> back.</summary>
    private static RefreshToken CreateLatestInFamily(
        Guid familyId, int secondsAgo, bool isRevoked = false) => new()
    {
        UserId    = 7,
        TokenHash = "successor-hash",
        FamilyId  = familyId,
        DeviceId  = DeviceId,
        IsRevoked = isRevoked,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        CreatedAt = DateTime.UtcNow.AddSeconds(-secondsAgo)
    };

    private void SetupRepo(RefreshToken stored, RefreshToken? latest)
    {
        _repoMock.Setup(r => r.GetRefreshTokenByHashAsync(TokenHash, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(stored);
        _repoMock.Setup(r => r.GetLatestTokenInFamilyAsync(stored.FamilyId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(latest);
    }

    private static RefreshRequest Request() => new(PlainToken, DeviceId);

    // ─────────────────────────────────────────────────
    // Concurrent refresh — must survive
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ConsumedTokenButFamilyRotatedJustNow_IssuesNewPairWithoutRevoking()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(
            CreateStoredToken(familyId, isConsumed: true),
            CreateLatestInFamily(familyId, secondsAgo: 2));

        var result = await CreateSut().RefreshAsync(Request());

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                         Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_ConsumedTokenAtEdgeOfGraceWindow_StillIssuesNewPair()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(
            CreateStoredToken(familyId, isConsumed: true),
            CreateLatestInFamily(familyId, secondsAgo: 55));

        var result = await CreateSut(graceSeconds: 60).RefreshAsync(Request());

        result.AccessToken.Should().Be("access-token");
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                         Times.Never);
    }

    // ─────────────────────────────────────────────────
    // Genuine reuse — must still revoke the family
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ConsumedTokenAndFamilyIdleBeyondGrace_RevokesFamily()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(
            CreateStoredToken(familyId, isConsumed: true),
            CreateLatestInFamily(familyId, secondsAgo: 600));

        var sut = CreateSut(graceSeconds: 60);

        await Assert.ThrowsAsync<InvalidTokenException>(() => sut.RefreshAsync(Request()));
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(familyId, It.IsAny<CancellationToken>()),
                         Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ConsumedTokenAndFamilyAlreadyRevoked_RevokesAndThrows()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(
            CreateStoredToken(familyId, isConsumed: true),
            CreateLatestInFamily(familyId, secondsAgo: 2, isRevoked: true));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidTokenException>(() => sut.RefreshAsync(Request()));
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(familyId, It.IsAny<CancellationToken>()),
                         Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_GraceDisabled_RevokesFamilyOnAnyReuse()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(
            CreateStoredToken(familyId, isConsumed: true),
            CreateLatestInFamily(familyId, secondsAgo: 1));

        var sut = CreateSut(graceSeconds: 0);

        await Assert.ThrowsAsync<InvalidTokenException>(() => sut.RefreshAsync(Request()));
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(familyId, It.IsAny<CancellationToken>()),
                         Times.Once);
    }

    // ─────────────────────────────────────────────────
    // Revoked token — rejected without a theft alert
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_RevokedToken_ThrowsWithoutRevokingAgain()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(CreateStoredToken(familyId, isConsumed: true, isRevoked: true), latest: null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidTokenException>(() => sut.RefreshAsync(Request()));
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                         Times.Never);
    }

    // ─────────────────────────────────────────────────
    // Normal rotation — unchanged behaviour
    // ─────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_UnconsumedToken_RotatesAndKeepsFamily()
    {
        var familyId = Guid.NewGuid();
        var stored   = CreateStoredToken(familyId);
        SetupRepo(stored, latest: null);

        var result = await CreateSut().RefreshAsync(Request());

        result.RefreshToken.Should().Be("new-refresh-token");
        stored.IsConsumed.Should().BeTrue();
        _repoMock.Verify(r => r.AddRefreshTokenAsync(
                             It.Is<RefreshToken>(t => t.FamilyId == familyId),
                             It.IsAny<CancellationToken>()),
                         Times.Once);
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                         Times.Never);
    }

    [Fact]
    public async Task RefreshAsync_DeviceIdMismatch_Throws()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(CreateStoredToken(familyId), latest: null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidTokenException>(
            () => sut.RefreshAsync(new RefreshRequest(PlainToken, "someone-elses-device")));
    }

    [Fact]
    public async Task RefreshAsync_DeactivatedUser_RevokesFamilyAndThrows()
    {
        var familyId = Guid.NewGuid();
        SetupRepo(CreateStoredToken(familyId, isActive: false), latest: null);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidTokenException>(() => sut.RefreshAsync(Request()));
        _repoMock.Verify(r => r.RevokeTokenFamilyAsync(familyId, It.IsAny<CancellationToken>()),
                         Times.Once);
    }
}
