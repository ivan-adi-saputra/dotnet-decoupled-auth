using System.Net;
using System.Net.Http.Json;
using AuthApp.Api.Models.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthApp.Api.Tests.Integration;

/// <summary>
/// Proves the HttpOnly cookie set on login is a real, working alternative to the
/// Authorization header — not just code that compiles. WebApplicationFactory's client has
/// HandleCookies enabled by default, so it behaves like a browser: a Set-Cookie from login
/// is automatically replayed on later requests through the same HttpClient instance.
/// </summary>
public class CookieAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CookieAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    }

    [Fact]
    public async Task Me_succeeds_from_the_cookie_alone_with_no_authorization_header()
    {
        var client = _factory.CreateClient();
        var username = $"cookie-{Guid.NewGuid():N}"[..32];
        const string password = "Secret123!";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));

        Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c =>
            c.StartsWith("AuthToken=", StringComparison.Ordinal) &&
            c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        // No Authorization header attached anywhere — the cookie the test client captured
        // from the login response above is all that authenticates this request.
        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<UserInfoResponse>();
        Assert.Equal(username, me!.Username);
    }

    [Fact]
    public async Task Logout_clears_the_cookie_so_a_later_me_call_is_unauthorized()
    {
        var client = _factory.CreateClient();
        var username = $"cookie-{Guid.NewGuid():N}"[..32];
        const string password = "Secret123!";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_without_ever_having_logged_in_still_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
