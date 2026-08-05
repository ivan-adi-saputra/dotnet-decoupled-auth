using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace AuthApp.Client.Services;

/// <summary>
/// Thin C# wrapper around the SweetAlert2 toast helpers in wwwroot/js/notifications.js.
/// Notifications are best-effort and purely cosmetic: a JS-side failure (SweetAlert2
/// didn't load, an ad-blocker interfered, a JS syntax error, etc.) must never propagate
/// to the caller — proven by a live test where an unhandled JS error froze the submit
/// button forever and blocked navigation entirely. Only JSException/TaskCanceledException
/// are swallowed here, since those are the interop failures that legitimately belong to
/// "the environment," not to a bug in this class — a genuine programmer error (e.g. a
/// null message) is left to throw so it isn't silently logged away as a warning.
/// </summary>
public class NotificationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IJSRuntime jsRuntime, ILogger<NotificationService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public Task ShowSuccessAsync(string message) => InvokeSafelyAsync("showSuccessToast", message);

    public Task ShowErrorAsync(string message) => InvokeSafelyAsync("showErrorToast", message);

    private async Task InvokeSafelyAsync(string jsFunctionName, string message)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(jsFunctionName, message);
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Notification '{Function}' failed; continuing without it.", jsFunctionName);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Notification '{Function}' timed out; continuing without it.", jsFunctionName);
        }
    }
}
