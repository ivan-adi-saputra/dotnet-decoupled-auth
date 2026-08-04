using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthApp.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthApp.Api.Tests.Services;

public class JwtTokenGeneratorTests
{
    private const string SigningKey = "unit-test-signing-key-at-least-32-bytes-long!!";

    private static JwtTokenGenerator CreateGenerator(int expiryMinutes = 60)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AuthApp.Tests",
                ["Jwt:Audience"] = "AuthApp.Tests.Client",
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString()
            })
            .Build();

        return new JwtTokenGenerator(configuration);
    }

    [Fact]
    public void GenerateToken_produces_a_well_formed_jwt()
    {
        var token = CreateGenerator().GenerateToken("alice");

        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateToken_embeds_the_username_as_the_name_claim()
    {
        var token = CreateGenerator().GenerateToken("alice");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Name && c.Value == "alice");
    }

    [Fact]
    public void GenerateToken_produces_a_token_that_passes_validation_with_the_same_settings()
    {
        var token = CreateGenerator().GenerateToken("alice");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AuthApp.Tests",
            ValidateAudience = true,
            ValidAudience = "AuthApp.Tests.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

        Assert.Equal("alice", principal.Identity?.Name);
    }

    [Fact]
    public void GenerateToken_sets_expiry_based_on_configuration()
    {
        var token = CreateGenerator(expiryMinutes: 5).GenerateToken("alice");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(5);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 10);
    }

    [Fact]
    public void GenerateToken_produces_a_token_that_fails_validation_once_expired()
    {
        // Already expired by the time it's validated below.
        var token = CreateGenerator(expiryMinutes: -1).GenerateToken("alice");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AuthApp.Tests",
            ValidateAudience = true,
            ValidAudience = "AuthApp.Tests.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // matches Program.cs's intent: prove expiry itself is enforced.
        };

        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_throws_a_clear_error_when_signing_key_is_missing(string? signingKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AuthApp.Tests",
                ["Jwt:Audience"] = "AuthApp.Tests.Client",
                ["Jwt:SigningKey"] = signingKey
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(configuration));
        Assert.Contains("Jwt:SigningKey", ex.Message);
    }

    [Fact]
    public void Constructor_throws_when_signing_key_is_shorter_than_32_bytes()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "AuthApp.Tests",
                ["Jwt:Audience"] = "AuthApp.Tests.Client",
                ["Jwt:SigningKey"] = "too-short"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(configuration));
    }

    [Theory]
    [InlineData(null, "AuthApp.Tests.Client")]
    [InlineData("AuthApp.Tests", null)]
    public void Constructor_throws_a_clear_error_when_issuer_or_audience_is_missing(string? issuer, string? audience)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = issuer,
                ["Jwt:Audience"] = audience,
                ["Jwt:SigningKey"] = SigningKey
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(configuration));
    }
}
