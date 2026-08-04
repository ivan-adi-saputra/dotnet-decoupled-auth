namespace AuthApp.Api.Models.Dtos;

/// <summary>
/// Uniform response shape for auth endpoints, mirroring the "Output Notification"
/// steps on the register/login flowcharts.
/// </summary>
public record AuthResponse(bool Success, string Message);
