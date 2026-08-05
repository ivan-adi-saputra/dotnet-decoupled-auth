namespace AuthApp.Api.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(string username);

    /// <summary>
    /// How long a generated token stays valid, in minutes. Exposed so callers (e.g. the
    /// cookie set on login) can align their own expiry with the token's, without reading
    /// Jwt:ExpiryMinutes from configuration a second time.
    /// </summary>
    int ExpiryMinutes { get; }
}
