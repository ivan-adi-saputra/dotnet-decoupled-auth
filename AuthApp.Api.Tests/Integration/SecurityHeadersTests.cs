using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthApp.Api.Tests.Integration;

public class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task Every_response_includes_X_Content_Type_Options_nosniff()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Contains("nosniff", values!);
    }
}
