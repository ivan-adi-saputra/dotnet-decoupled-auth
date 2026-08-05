using System.Net;
using System.Net.Http.Json;
using AuthApp.Api.Models.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthApp.Api.Tests.Integration;

/// <summary>
/// Proves the server-side rate limiter on /login and /register actually rejects requests
/// once the per-IP limit is exceeded — closing the gap where LoginFailed (a purely
/// client-side counter) does nothing to stop someone calling these endpoints directly.
/// Uses its own WebApplicationFactory instance (not shared via a class fixture) so its
/// rate-limiter state — a DI singleton scoped to that host — can't affect, or be affected
/// by, request counts from any other test class.
/// </summary>
public class RateLimitingTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    [Fact]
    public async Task Login_returns_429_after_exceeding_the_per_ip_limit()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // The configured limit is 10 requests/minute/IP. All requests in this test share
        // one HttpClient (and therefore one client IP as seen by the in-process TestServer),
        // so the 11th is guaranteed to exceed it regardless of credentials — invalid login
        // attempts still count against the limit, since rate limiting runs before the
        // request reaches the controller at all.
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody", "wrong-password"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task Register_returns_429_after_exceeding_the_per_ip_limit()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 11; i++)
        {
            // A distinct username per call so the 429 isn't masked by an unrelated 409
            // (duplicate username) — the point here is proving the limiter itself, not
            // register's business logic.
            lastResponse = await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest($"ratelimit{i}", "secret123"));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    [Fact]
    public async Task Requests_within_the_limit_are_not_rejected()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody", "wrong-password"));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
