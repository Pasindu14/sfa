using sfa_api.Features.Auth.Entities;

namespace sfa_api.Features.Users.Entities;

public class User
{
    public int Id { get; set; }                              // int — auto increment, fast joins
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.SalesRep;
    public string? DeviceId { get; set; }                    // Mobile device identifier
    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
