using AuthApp.Api.Authentication;
using AuthApp.Api.Models;
using AuthApp.Api.Models.Dtos;
using AuthApp.Api.RateLimiting;
using AuthApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IJwtTokenValidator _tokenValidator;
    private readonly ITokenRevocationStore _revocationStore;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserStore userStore,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IJwtTokenValidator tokenValidator,
        ITokenRevocationStore revocationStore,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenValidator = tokenValidator;
        _revocationStore = revocationStore;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimiterPolicies.Auth)]
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
            return Conflict(new AuthResponse(false, $"Username '{request.Username}' is already taken."));
        }

        _logger.LogInformation("User {Username} registered successfully.", request.Username);
        return Ok(new AuthResponse(true, "Registration successful. You can now log in."));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimiterPolicies.Auth)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var user = _userStore.FindByUsername(request.Username);

        // Always pay the cost of a real PBKDF2 verify, whether or not a user was found —
        // verifying against a dummy hash when there's no real one keeps response timing
        // uniform, so it can't be used to tell which usernames are registered. The
        // "Username not found."/"Incorrect password." messages still differ (an already
        // reviewed, deliberate trade-off from Sprint 3); this only removes the additional,
        // independent timing side-channel measured on top of it.
        var passwordIsValid = _passwordHasher.Verify(request.Password, user?.PasswordHash ?? _passwordHasher.DummyHash);

        if (user is null)
        {
            _logger.LogWarning("Login failed: username {Username} not found.", request.Username);
            return Unauthorized(new LoginResponse(false, "Username not found."));
        }

        if (!passwordIsValid)
        {
            _logger.LogWarning("Login failed: incorrect password for username {Username}.", request.Username);
            return Unauthorized(new LoginResponse(false, "Incorrect password."));
        }

        var token = _tokenGenerator.GenerateToken(user.Username);

        // Set alongside the token in the response body (kept for Swagger's manual
        // "Authorize" flow) so the SPA can rely on the cookie instead of holding the raw
        // token itself — the cookie is HttpOnly, so it survives a page reload without ever
        // being readable by JavaScript, unlike localStorage/sessionStorage.
        Response.Cookies.Append(AuthCookieDefaults.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_tokenGenerator.ExpiryMinutes),
            Path = "/"
        });

        _logger.LogInformation("User {Username} logged in successfully.", request.Username);
        return Ok(new LoginResponse(true, "Login successful. Welcome back!", token));
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        // Deliberately not [Authorize]: logging out should always succeed, even if the
        // cookie is already missing or the token inside it has expired/was tampered with.
        var authHeader = Request.Headers.Authorization.ToString();
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : Request.Cookies[AuthCookieDefaults.CookieName];

        // Only revoke a jti extracted from a token that actually validates against this
        // server's own signing key — otherwise anyone could force-logout an arbitrary
        // session just by sending a request with a fabricated token containing someone
        // else's real jti (which isn't secret; it can appear in logs, etc.).
        if (!string.IsNullOrEmpty(token) && _tokenValidator.TryValidate(token, out var jti, out var expiresAt))
        {
            _revocationStore.Revoke(jti, expiresAt);
        }

        Response.Cookies.Delete(AuthCookieDefaults.CookieName, new CookieOptions { Path = "/" });
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserInfoResponse> Me()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        return Ok(new UserInfoResponse(username));
    }
}
