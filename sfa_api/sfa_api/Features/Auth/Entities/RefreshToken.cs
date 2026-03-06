using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Auth.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }                          // int FK — matches User.Id
    public string TokenHash { get; set; } = string.Empty;   // Stored hashed — never plain
    public Guid FamilyId { get; set; }                       // All tokens in a session share this
    public string DeviceId { get; set; } = string.Empty;
    public bool IsConsumed { get; set; } = false;            // Used once then consumed
    public bool IsRevoked { get; set; } = false;             // Revoked on logout
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
