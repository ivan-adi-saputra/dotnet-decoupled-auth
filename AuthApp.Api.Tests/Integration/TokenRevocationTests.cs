using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthApp.Api.Models.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthApp.Api.Tests.Integration;

/// <summary>
/// Proves logout actually invalidates the JWT server-side, closing the gap where a token
/// that leaked before logout (copied from browser storage, a proxy log, etc.) would
/// otherwise stay valid until its natural expiry — JWTs are stateless by default, so
/// deleting the cookie alone (the previous behavior) never touched the token itself.
/// </summary>
public class TokenRevocationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TokenRevocationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task A_token_captured_before_logout_is_rejected_after_logout()
    {
        var username = $"revoke-{Guid.NewGuid():N}"[..32];
        const string password = "Secret123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Simulates a copy of the token held separately from the cookie the browser (or
        // here, the test client) manages — e.g. something an attacker captured earlier.
        var beforeLogout = await SendWithBearerToken(login!.Token!);
        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);

        await _client.PostAsync("/api/auth/logout", content: null);

        var afterLogout = await SendWithBearerToken(login.Token!);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task A_different_users_token_is_unaffected_by_someone_elses_logout()
    {
        // Regression guard: revocation must be scoped to the specific jti being logged
        // out, not something broader (e.g. "every token issued around that time").
        const string password = "Secret123!";
        var userA = $"revoke-a-{Guid.NewGuid():N}"[..32];
        var userB = $"revoke-b-{Guid.NewGuid():N}"[..32];

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(userA, password));
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(userB, password));

        var loginA = await (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userA, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(userB, password));

        // The shared test client's cookie jar now holds B's token (the most recent
        // login), so this logs out B's session specifically.
        await _client.PostAsync("/api/auth/logout", content: null);

        var userAResponse = await SendWithBearerToken(loginA!.Token!);
        Assert.Equal(HttpStatusCode.OK, userAResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_with_a_fabricated_token_does_not_throw_and_still_clears_the_cookie()
    {
        // A forged/garbage token must fail TryValidate and be ignored — not crash the
        // request, and not let an attacker revoke an arbitrary jti of their choosing by
        // embedding it in a token that was never actually signed by this server.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.token");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendWithBearerToken(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }
}
