namespace sfa_api.Common.Audit;

public class RevokedToken
{
    public string Jti { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
