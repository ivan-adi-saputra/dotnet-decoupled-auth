using System.Collections.Concurrent;

namespace AuthApp.Api.Services;

/// <summary>
/// In-memory denylist of revoked jti's, registered as a singleton so it survives across
/// requests for the lifetime of the app — consistent with InMemoryUserStore's lifetime and
/// the project's overall "no persistence needed for this test" design.
/// </summary>
public class InMemoryTokenRevocationStore : ITokenRevocationStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _revokedUntil = new();

    public void Revoke(string jti, DateTimeOffset expiresAt)
    {
        _revokedUntil[jti] = expiresAt;
        PruneExpired();
    }

    public bool IsRevoked(string jti)
    {
        if (!_revokedUntil.TryGetValue(jti, out var expiresAt))
        {
            return false;
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            _revokedUntil.TryRemove(jti, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Opportunistic cleanup on every write, so this dictionary doesn't grow forever —
    /// entries past their own token's expiry are no longer useful (normal lifetime
    /// validation already rejects that token) and safe to drop.
    /// </summary>
    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (jti, expiresAt) in _revokedUntil)
        {
            if (expiresAt <= now)
            {
                _revokedUntil.TryRemove(jti, out _);
            }
        }
    }
}
