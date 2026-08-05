using System.Net.Http.Headers;
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
    /// Attaches (or clears, when null) the bearer token used for subsequent authenticated
    /// calls such as GET /api/auth/me.
    /// </summary>
    public void SetBearerToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }
}
