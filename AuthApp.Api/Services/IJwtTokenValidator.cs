namespace AuthApp.Api.Services;

public interface IJwtTokenValidator
{
    /// <summary>
    /// Validates the token's signature, issuer, audience, and lifetime — the same checks
    /// JwtBearer applies to every authenticated request. Used by Logout, which can't rely
    /// on [Authorize] (logout must succeed even for an expired/tampered token) but still
    /// needs to trust the jti it revokes actually came from this server, not something an
    /// attacker fabricated to force-revoke someone else's session.
    /// </summary>
    bool TryValidate(string token, out string jti, out DateTimeOffset expiresAt);
}
