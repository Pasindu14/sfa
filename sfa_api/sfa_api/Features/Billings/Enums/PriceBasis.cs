namespace sfa_api.Features.Billings.Enums;

/// <summary>
/// Which structure price a bill line was priced from. Stored as the member name (max 10 chars).
/// </summary>
public enum PriceBasis
{
    /// <summary>Structure's dealer pack price.</summary>
    Pack,
    /// <summary>Structure's dealer case price, spread over the packs in a case.</summary>
    Case,
    /// <summary>A price the rep typed (return lines).</summary>
    Manual
}
