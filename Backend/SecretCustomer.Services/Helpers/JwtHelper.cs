using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Services.Helpers;

public class JwtHelper
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;

    public JwtHelper(string secretKey, string issuer, string audience, int expirationMinutes)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _expirationMinutes = expirationMinutes;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Impersonation token olusturur - orijinal kullanici bilgilerini korur
    /// </summary>
    public string GenerateImpersonationToken(User originalUser, int customerId, string customerName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, originalUser.Id.ToString()),
            new Claim(ClaimTypes.Name, originalUser.Username),
            new Claim(ClaimTypes.Email, originalUser.Email),
            new Claim(ClaimTypes.Role, originalUser.Role.ToString()),
            // Impersonation claims
            new Claim("IsImpersonating", "true"),
            new Claim("ImpersonatedCustomerId", customerId.ToString()),
            new Claim("ImpersonatedCustomerName", customerName),
            new Claim("OriginalUserId", originalUser.Id.ToString()),
            new Claim("OriginalUserName", $"{originalUser.FirstName} {originalUser.LastName}"),
            new Claim("OriginalRole", originalUser.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// CustomerPersonnel için token oluşturur
    /// </summary>
    public string GenerateCustomerPersonnelToken(CustomerPersonnel personnel)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, personnel.Id.ToString()),
            new Claim(ClaimTypes.Name, personnel.Username),
            new Claim(ClaimTypes.Email, personnel.Email),
            new Claim(ClaimTypes.Role, personnel.Role.ToString()),
            new Claim("CustomerId", personnel.CustomerId.ToString()),
            new Claim("CustomerName", personnel.Customer?.CompanyName ?? ""),
            new Claim("FullName", personnel.FullName),
            new Claim("UserType", "CustomerPersonnel")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
