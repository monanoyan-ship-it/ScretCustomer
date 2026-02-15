using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SecretCustomer.Tests.Api.Helpers;

/// <summary>
/// Test ortamında JWT token üretmek için helper.
/// </summary>
public static class TestAuthHelper
{
    private const string TestSecretKey = "YourSuperSecretKeyForJWT_MinLength32Characters!";
    private const string TestIssuer = "SecretCustomerAPI";
    private const string TestAudience = "SecretCustomerClient";

    public static string GenerateToken(int userId, string username, string role, int? customerId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("UserId", userId.ToString())
        };

        if (customerId.HasValue)
            claims.Add(new Claim("CustomerId", customerId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string AdminToken(int userId = 1) =>
        GenerateToken(userId, "admin", "Admin");

    public static string QualitySpecialistToken(int userId = 2) =>
        GenerateToken(userId, "specialist", "QualitySpecialist");

    public static string FieldWorkerToken(int userId = 3) =>
        GenerateToken(userId, "fieldworker", "FieldWorker");
}
