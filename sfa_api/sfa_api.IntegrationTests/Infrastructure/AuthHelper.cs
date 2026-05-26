using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Generates JWT tokens for integration tests using the same secret/issuer/audience
/// as the real app (from appsettings.json).
/// </summary>
public static class AuthHelper
{
    // Must match appsettings.json values
    private const string SecretKey = "a8F#9kLm2PqR7tVxY4zW!6nB@3cD$5Gh";
    private const string Issuer = "SFA.API";
    private const string Audience = "SFA.Clients";

    public static string GenerateToken(int userId, string role, string email = "test@sfa.com", string name = "Test User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(ClaimTypes.Role, role),
            new Claim("deviceId", "")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Admin token for userId=100 (avoids conflict with seeded admin userId=1)</summary>
    public static string AdminToken => GenerateToken(100, "Admin", "admin-test@sfa.com", "Test Admin");

    /// <summary>SalesRep token for userId=200</summary>
    public static string SalesRepToken => GenerateToken(200, "SalesRep", "rep@sfa.com", "Test Rep");

    /// <summary>Manager token for userId=300 — intentionally uses non-existent role to produce 403 in auth tests</summary>
    public static string ManagerToken => GenerateToken(300, "Manager", "manager@sfa.com", "Test Manager");

    /// <summary>Supervisor token for userId=400 — used in PurchaseOrder workflow tests (approve, manager-edit)</summary>
    public static string SupervisorToken => GenerateToken(400, "Supervisor", "supervisor@sfa.com", "Test Supervisor");
}
