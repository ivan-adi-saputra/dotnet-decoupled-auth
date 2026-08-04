using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthApp.Api.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _issuer = RequireConfig(configuration, "Jwt:Issuer");
        _audience = RequireConfig(configuration, "Jwt:Audience");
        _expiryMinutes = configuration.GetValue("Jwt:ExpiryMinutes", 60);

        var signingKey = RequireConfig(configuration, "Jwt:SigningKey");
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 bytes (256 bits) for HMAC-SHA256.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public string GenerateToken(string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Defends this class's own precondition instead of trusting that Program.cs already
    /// validated configuration — so constructing this class anywhere else (a future tool,
    /// a test that forgets to set config) fails with a clear message, not a cryptic
    /// ArgumentNullException from deep inside token generation.
    /// </summary>
    private static string RequireConfig(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} must be configured.");
        }

        return value;
    }
}
