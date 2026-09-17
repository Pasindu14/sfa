using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace sfa_api.Features.MobileSync;

/// <summary>
/// Conditional-GET support for the mobile catalog endpoints.
/// <para>
/// The ETag is a weak validator over the SERIALIZED CATALOG ROWS (not the envelope, and not
/// <c>CachedAt</c>, which changes on every cache refill without the data changing). Hashing the rows
/// rather than a timestamp/count version means it changes for every visible difference — a row added,
/// edited, deactivated (drops out), a price change, or a category rename flowing into CategoryName —
/// and never for invisible ones. It always describes exactly the body served alongside it.
/// Weak (<c>W/</c>) because response compression and the envelope's CachedAt/traceId vary the bytes.
/// </para>
/// Contract (mobile relies on it): <c>ETag</c> on 200; <c>If-None-Match</c> match → 304, empty body.
/// </summary>
public static class MobileSyncETag
{
    private const string Version = "v1:";

    public static string Compute<T>(T rows)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(rows);
        var prefix = System.Text.Encoding.UTF8.GetBytes(Version);
        var buffer = new byte[prefix.Length + json.Length];
        prefix.CopyTo(buffer, 0);
        json.CopyTo(buffer, prefix.Length);
        var hash = SHA256.HashData(buffer);
        return $"W/\"{Convert.ToHexString(hash, 0, 16).ToLowerInvariant()}\"";
    }

    /// <summary>
    /// True when the request's If-None-Match matches <paramref name="etag"/> using weak comparison
    /// (RFC 9110 §13.1.2), including <c>*</c> and comma-separated lists. No header → false.
    /// </summary>
    public static bool IfNoneMatchMatches(HttpRequest request, string etag)
    {
        var raw = request.Headers.IfNoneMatch;
        if (raw.Count == 0) return false;
        if (!EntityTagHeaderValue.TryParseList(raw, out var candidates)) return false;

        var current = EntityTagHeaderValue.Parse(etag);
        return candidates.Any(c => c.Equals(EntityTagHeaderValue.Any) || c.Compare(current, useStrongComparison: false));
    }
}
