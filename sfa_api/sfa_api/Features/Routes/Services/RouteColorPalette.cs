using System.Text.RegularExpressions;

namespace sfa_api.Features.Routes.Services;

/// <summary>
/// Resolves the pin colour to persist for a route so that every route ends up with a
/// unique colour. A caller-supplied colour is honoured only when it is a valid hex code
/// that no other route is already using; otherwise the first unused colour from a curated
/// palette of visually-distinct, map-friendly hues is assigned, falling back to a random
/// unused hex code once the palette is exhausted.
/// </summary>
public static partial class RouteColorPalette
{
    // Curated, visually-distinct palette (Tailwind-style 500/600 hues). Early routes get
    // maximally-different colours before we ever need to reach for a random fallback.
    public static readonly IReadOnlyList<string> Colors = new[]
    {
        "#3B82F6", "#EF4444", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899",
        "#14B8A6", "#F97316", "#6366F1", "#84CC16", "#06B6D4", "#D946EF",
        "#EAB308", "#22C55E", "#0EA5E9", "#A855F7", "#F43F5E", "#65A30D",
        "#0891B2", "#7C3AED", "#DB2777", "#059669", "#DC2626", "#2563EB",
    };

    [GeneratedRegex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColorRegex();

    /// <summary>
    /// Resolves the colour to persist. <paramref name="usedColors"/> must already exclude
    /// the route being updated so that a route keeps its own colour on edit. The set should
    /// be case-insensitive (see <see cref="StringComparer.OrdinalIgnoreCase"/>).
    /// </summary>
    public static string Resolve(string? requested, ISet<string> usedColors)
    {
        // 1. Honour a valid, unused caller-supplied colour.
        var normalized = Normalize(requested);
        if (normalized is not null && !usedColors.Contains(normalized))
            return normalized;

        // 2. First unused colour from the curated palette.
        foreach (var color in Colors)
            if (!usedColors.Contains(color))
                return color;

        // 3. Palette exhausted — generate a random unused hex code.
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var candidate = RandomHex();
            if (!usedColors.Contains(candidate))
                return candidate;
        }

        // Statistically unreachable with 16.7M colours; guarantees a non-null return.
        return RandomHex();
    }

    /// <summary>Uppercased hex if valid; otherwise null (blank / non-hex is treated as "not supplied").</summary>
    public static string? Normalize(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        var trimmed = color.Trim();
        return HexColorRegex().IsMatch(trimmed) ? trimmed.ToUpperInvariant() : null;
    }

    private static string RandomHex() => $"#{Random.Shared.Next(0, 0x1000000):X6}";
}
