namespace AuthApp.Api.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);

    /// <summary>
    /// A fixed hash not tied to any real user, computed once. Callers verify against this
    /// when no matching user was found, so a login attempt for a nonexistent username
    /// costs the same as one for a real username with the wrong password — otherwise the
    /// response time itself leaks which usernames are registered (measured: ~3-13ms when
    /// no hash check runs at all vs ~60ms when it does).
    /// </summary>
    string DummyHash { get; }
}
