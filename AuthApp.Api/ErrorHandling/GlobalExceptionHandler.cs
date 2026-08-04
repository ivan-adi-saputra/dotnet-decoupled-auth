using AuthApp.Api.Models.Dtos;
using Microsoft.AspNetCore.Diagnostics;

namespace AuthApp.Api.ErrorHandling;

/// <summary>
/// Catches any exception that escapes the MVC pipeline, logs it, and returns a uniform
/// error response instead of leaking a stack trace or an empty 500.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            new AuthResponse(false, "An unexpected error occurred."),
            cancellationToken);

        return true;
    }
}
