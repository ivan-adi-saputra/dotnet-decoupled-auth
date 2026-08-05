namespace AuthApp.Api.Authentication;

/// <summary>
/// Shared between Program.cs (JwtBearer's cookie fallback) and AuthController (setting and
/// clearing the cookie), so the name can't drift out of sync between the two.
/// </summary>
public static class AuthCookieDefaults
{
    public const string CookieName = "AuthToken";
}
