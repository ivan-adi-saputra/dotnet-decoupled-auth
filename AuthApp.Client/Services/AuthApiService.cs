using System.Net.Http.Json;
using System.Text.Json;
using AuthApp.Client.Models;

namespace AuthApp.Client.Services;

/// <summary>
/// Thin wrapper around HttpClient for the backend auth endpoints. Pages call this
/// instead of injecting HttpClient directly, per Sprint 4's requirement.
/// </summary>
public class AuthApiService
{
    private readonly HttpClient _httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse> RegisterAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Register, new RegisterRequest(username, password));
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return result ?? new AuthResponse(false, "Unexpected response from the server.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new AuthResponse(false, DescribeFailure(ex));
        }
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Login, new LoginRequest(username, password));
            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return result ?? new LoginResponse(false, "Unexpected response from the server.", null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new LoginResponse(false, DescribeFailure(ex), null);
        }
    }

    /// <summary>
    /// Maps the network/deserialization failures caught above to a single user-facing
    /// message per exception type, kept in one place so RegisterAsync and LoginAsync
    /// can't drift out of sync with each other.
    /// </summary>
    private static string DescribeFailure(Exception ex) => ex switch
    {
        HttpRequestException => "Unable to reach the server. Please try again.",
        TaskCanceledException => "The request timed out. Please try again.",
        JsonException => "Received an unexpected response from the server.",
        _ => "An unexpected error occurred. Please try again."
    };

    /// <summary>
    /// Asks the server who (if anyone) the caller's session cookie identifies. Used to
    /// restore AuthSession when the app boots — the cookie is HttpOnly, so this is the
    /// only way the client can find out it survived a reload. Returns null for any
    /// failure (no cookie, expired token, network error) — "not logged in" is a normal
    /// outcome here, not something callers need to distinguish from a real error.
    /// </summary>
    public async Task<UserInfoResponse?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiRoutes.Me);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<UserInfoResponse>()
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Clears the server-side auth cookie. Best-effort: even if this fails (e.g. the API
    /// is briefly unreachable), the caller still clears its own in-memory AuthSession,
    /// which is what the app's route guards actually check.
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync(ApiRoutes.Logout, content: null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
        }
    }
}
