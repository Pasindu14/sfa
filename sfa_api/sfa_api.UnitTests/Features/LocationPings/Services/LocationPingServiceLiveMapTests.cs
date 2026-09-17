using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using sfa_api.Features.LocationPings.DTOs;
using sfa_api.Features.LocationPings.Repositories;
using sfa_api.Features.LocationPings.Services;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Caching;

namespace sfa_api.UnitTests.Features.LocationPings.Services;

public class LocationPingServiceLiveMapTests
{
    private readonly Mock<ILocationPingRepository> _repo = new();

    private LocationPingService CreateService(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new LocationPingService(_repo.Object, Mock.Of<IUserRepository>(), Mock.Of<ICacheService>(), config);
    }

    [Fact]
    public async Task GetLatestPerRep_NoConfig_IsUnbounded_PreservingOriginalBehaviour()
    {
        DateTimeOffset? captured = DateTimeOffset.MinValue;
        _repo.Setup(r => r.GetLatestPerRepAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
             .Callback<DateTimeOffset?, CancellationToken>((s, _) => captured = s)
             .ReturnsAsync(new List<RepLocationPingDto>());

        await CreateService(new()).GetLatestPerRepAsync();

        captured.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestPerRep_PositiveWindow_BoundsInUtc()
    {
        DateTimeOffset? captured = null;
        _repo.Setup(r => r.GetLatestPerRepAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
             .Callback<DateTimeOffset?, CancellationToken>((s, _) => captured = s)
             .ReturnsAsync(new List<RepLocationPingDto>());

        var before = DateTimeOffset.UtcNow;
        await CreateService(new() { ["LocationPings:LiveMapWindowHours"] = "24" }).GetLatestPerRepAsync();

        captured.Should().NotBeNull();
        captured!.Value.Offset.Should().Be(TimeSpan.Zero);
        captured.Value.Should().BeCloseTo(before.AddHours(-24), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetLatestPerRep_WindowZero_IsUnbounded()
    {
        DateTimeOffset? captured = DateTimeOffset.MinValue;
        _repo.Setup(r => r.GetLatestPerRepAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
             .Callback<DateTimeOffset?, CancellationToken>((s, _) => captured = s)
             .ReturnsAsync(new List<RepLocationPingDto>());

        await CreateService(new() { ["LocationPings:LiveMapWindowHours"] = "0" }).GetLatestPerRepAsync();

        captured.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestPerRep_ReturnsRepositoryRowsUnchanged()
    {
        var row = new RepLocationPingDto(7, "Rep", 6.9, 79.8, 5f, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _repo.Setup(r => r.GetLatestPerRepAsync(It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<RepLocationPingDto> { row });

        var result = await CreateService(new() { ["LocationPings:LiveMapWindowHours"] = "6" }).GetLatestPerRepAsync();

        result.Should().ContainSingle().Which.Should().Be(row);
    }
}
