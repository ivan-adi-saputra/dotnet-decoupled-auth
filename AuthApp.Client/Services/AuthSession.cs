namespace AuthApp.Client.Services;

/// <summary>
/// Holds the current user's username in memory only. The actual auth token now lives in
/// an HttpOnly cookie the browser manages on its own (never readable from C#/JS), so this
/// class no longer needs to hold it — a page reload clears this in-memory state, but
/// App.razor restores it at startup by asking the server who the cookie belongs to.
/// </summary>
public class AuthSession
{
    public string? Username { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Username);

    public void SignIn(string username)
    {
        Username = username;
    }

    public void SignOut()
    {
        Username = null;
    }
}
