namespace AuthApp.Client.Services;

/// <summary>
/// Backend endpoint paths, relative to the HttpClient's BaseAddress configured in Program.cs.
/// Consumed by the auth service implemented in Sprint 4.
/// </summary>
public static class ApiRoutes
{
    public const string Register = "api/auth/register";
    public const string Login = "api/auth/login";
    public const string Me = "api/auth/me";
}
