using FluentAssertions;
using sfa_api.Features.Routes.Services;

namespace sfa_api.UnitTests.Features.Routes.Services;

public class RouteColorPaletteTests
{
    private static HashSet<string> Used(params string[] colors)
        => new(colors, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Resolve_ValidUnusedRequestedColor_IsHonoured()
    {
        var result = RouteColorPalette.Resolve("#123ABC", Used());

        result.Should().Be("#123ABC");
    }

    [Fact]
    public void Resolve_RequestedColorLowercase_IsNormalisedToUppercase()
    {
        var result = RouteColorPalette.Resolve("#abcdef", Used());

        result.Should().Be("#ABCDEF");
    }

    [Fact]
    public void Resolve_RequestedColorAlreadyUsed_FallsBackToPalette()
    {
        // Requested colour is taken (case-insensitively) — should not be returned.
        var result = RouteColorPalette.Resolve("#3b82f6", Used("#3B82F6"));

        result.Should().NotBe("#3B82F6");
        RouteColorPalette.Colors.Should().Contain(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("not-a-color")]
    public void Resolve_BlankOrInvalidRequest_AssignsFirstUnusedPaletteColor(string? requested)
    {
        var result = RouteColorPalette.Resolve(requested, Used());

        result.Should().Be(RouteColorPalette.Colors[0]);
    }

    [Fact]
    public void Resolve_SkipsUsedPaletteColors_ReturnsFirstAvailable()
    {
        var used = Used(RouteColorPalette.Colors[0], RouteColorPalette.Colors[1]);

        var result = RouteColorPalette.Resolve(null, used);

        result.Should().Be(RouteColorPalette.Colors[2]);
    }

    [Fact]
    public void Resolve_EntirePaletteUsed_ReturnsRandomUnusedHex()
    {
        var used = new HashSet<string>(RouteColorPalette.Colors, StringComparer.OrdinalIgnoreCase);

        var result = RouteColorPalette.Resolve(null, used);

        result.Should().MatchRegex("^#[0-9A-F]{6}$");
        used.Should().NotContain(result);
    }

    [Fact]
    public void Colors_AreAllDistinct()
    {
        RouteColorPalette.Colors.Should().OnlyHaveUniqueItems();
    }
}
