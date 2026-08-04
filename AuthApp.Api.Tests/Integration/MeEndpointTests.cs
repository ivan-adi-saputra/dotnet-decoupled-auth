using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AuthApp.Api.Models.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthApp.Api.Tests.Integration;

/// <summary>
/// Hits the real HTTP pipeline (via WebApplicationFactory) instead of calling controller
/// methods directly, so [Authorize] is actually exercised — a plain unit test that calls
/// AuthController.Me() bypasses attribute-based authorization entirely, since that's
/// enforced by ASP.NET Core's middleware, not by the method body.
/// </summary>
public class MeEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task Me_without_a_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_a_garbage_token_returns_unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.token");

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_a_token_from_a_real_login_returns_the_username()
    {
        var username = $"integration-{Guid.NewGuid():N}";
        const string password = "Secret123!";

        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password));
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        var meResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<UserInfoResponse>();
        Assert.Equal(username, me!.Username);
    }
}
