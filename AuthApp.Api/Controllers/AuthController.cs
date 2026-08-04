using AuthApp.Api.Models;
using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserStore userStore, IPasswordHasher passwordHasher, ILogger<AuthController> logger)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status409Conflict)]
    public ActionResult<AuthResponse> Register([FromBody] RegisterRequest request)
    {
        var user = new User
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        if (!_userStore.TryAdd(user))
        {
            _logger.LogWarning("Register failed: username {Username} already exists.", request.Username);
            return Conflict(new AuthResponse(false, "Notification failed to register."));
        }

        _logger.LogInformation("User {Username} registered successfully.", request.Username);
        return Ok(new AuthResponse(true, "Notification success to register."));
    }
}
