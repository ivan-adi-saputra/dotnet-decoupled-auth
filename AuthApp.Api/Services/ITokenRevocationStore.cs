namespace AuthApp.Api.Services;

/// <summary>
/// Tracks JWT ids (jti) that have been explicitly logged out, so a token that leaked
/// before logout (copied from browser storage, a proxy log, etc.) can't keep working until
/// its natural expiry — JWTs are otherwise stateless and can't be invalidated any other way
/// short of waiting them out or rotating the signing key for everyone.
/// </summary>
public interface ITokenRevocationStore
{
    /// <summary>
    /// Marks a jti as revoked until <paramref name="expiresAt"/> — matching the token's own
    /// expiry, since a revoked-but-already-expired entry serves no purpose (the token would
    /// already be rejected by normal lifetime validation).
    /// </summary>
    void Revoke(string jti, DateTimeOffset expiresAt);

    bool IsRevoked(string jti);
}
