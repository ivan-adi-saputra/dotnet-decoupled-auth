namespace AuthApp.Api.Models.Dtos;

/// <summary>
/// Login-specific response: same shape as AuthResponse, plus a JWT issued only on
/// success. Token is null for every failure case (invalid credentials, validation error).
/// </summary>
public record LoginResponse(bool Success, string Message, string? Token = null)
    : AuthResponse(Success, Message);
