namespace AuthApp.Client.Models;

public record LoginResponse(bool Success, string Message, string? Token);
