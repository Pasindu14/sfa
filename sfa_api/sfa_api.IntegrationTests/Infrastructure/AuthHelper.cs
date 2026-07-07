using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Generates JWT tokens for integration tests using a dedicated test-only signing key.
/// The factory (<see cref="SfaWebApplicationFactory"/>) injects this same key as
/// Jwt:SecretKey so the app under test validates these tokens. This key is NOT the
/// production secret and must never be used outside tests.
/// </summary>
public static class AuthHelper
{
    // Test-only signing key — injected into the test host via SfaWebApplicationFactory.
    // Not a real secret; safe to commit. Must be >= 32 bytes for HMAC-SHA256.
    public const string SecretKey = "integration-tests-only-signing-key-not-for-production-0123456789";
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
