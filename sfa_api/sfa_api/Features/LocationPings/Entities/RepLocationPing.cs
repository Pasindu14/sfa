using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.LocationPings.Entities;

public class RepLocationPing
{
    public long Id { get; set; }

    public int RepId { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public float Accuracy { get; set; }

    /// <summary>Device clock — the real moment the position was captured.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Server clock — stamped on receipt; source of truth for ordering.</summary>
    public DateTimeOffset ReceivedAt { get; set; }

    // Navigation
    public User? Rep { get; set; }
}
