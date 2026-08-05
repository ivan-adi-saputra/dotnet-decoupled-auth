namespace AuthApp.Client.Services;

/// <summary>
/// Holds the current user's JWT and username in memory only — intentionally not
/// persisted to localStorage/sessionStorage, so a page reload clears it. This matches
/// the "reload = fresh Start" behavior already established for LoginFailed in Sprint 1/3.
/// </summary>
public class AuthSession
{
    public string? Username { get; private set; }
    public string? Token { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public void SignIn(string username, string token)
    {
        Username = username;
        Token = token;
    }

    public void SignOut()
    {
        Username = null;
        Token = null;
    }
}
