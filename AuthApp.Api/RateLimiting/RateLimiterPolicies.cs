namespace AuthApp.Api.RateLimiting;

/// <summary>
/// Shared between Program.cs (where the policy is configured) and AuthController (where
/// it's applied via [EnableRateLimiting]), so the name can't drift out of sync between
/// the two the way a bare string literal in each place could.
/// </summary>
public static class RateLimiterPolicies
{
    public const string Auth = "auth";
}
