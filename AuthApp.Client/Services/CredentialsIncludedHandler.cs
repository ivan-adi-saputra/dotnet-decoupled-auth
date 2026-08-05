using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AuthApp.Client.Services;

/// <summary>
/// Ensures every request to the API includes the HttpOnly auth cookie. The client and API
/// run on different ports (cross-origin), and browsers don't send credentials cross-origin
/// unless explicitly told to on each request.
/// </summary>
public class CredentialsIncludedHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, cancellationToken);
    }
}
