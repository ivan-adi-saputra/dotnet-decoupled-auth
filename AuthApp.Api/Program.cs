using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;
using AuthApp.Api.Authentication;
using AuthApp.Api.ErrorHandling;
using AuthApp.Api.Filters;
using AuthApp.Api.RateLimiting;
using AuthApp.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string ClientCorsPolicy = "AuthAppClient";

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/auth/login (without the \"Bearer \" prefix)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// FluentValidation (via ValidationFilter) is the single source of truth for request
// validation. Disable ASP.NET Core's automatic ModelState validation so a future
// DataAnnotation on a DTO can't silently introduce a second, differently-shaped
// error response alongside it.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
builder.Services.AddSingleton<IJwtTokenValidator, JwtTokenValidator>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];

// Fail fast in every environment — a missing Issuer/Audience/SigningKey wouldn't crash
// at startup otherwise, it would silently issue/validate malformed tokens and only
// surface as a confusing "every request is 401" symptom at runtime.
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must both be configured.");
}

if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be configured with at least 32 bytes (256 bits) for HMAC-SHA256.");
}

// Registered as its own singleton (not just assigned inline below) so JwtTokenValidator —
// used by Logout to safely trust a jti before revoking it — validates against the exact
// same rules as the JwtBearer middleware that protects every other endpoint, rather than a
// second copy of this config that could quietly drift out of sync.
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = jwtIssuer,
    ValidateAudience = true,
    ValidAudience = jwtAudience,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromSeconds(30)
};
builder.Services.AddSingleton(tokenValidationParameters);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = tokenValidationParameters;

        options.Events = new JwtBearerEvents
        {
            // The SPA no longer attaches an Authorization header itself — it relies on the
            // HttpOnly cookie set on login, which the browser sends automatically. Falling
            // back to the cookie only when no header is present keeps the existing
            // header-based flow (e.g. Swagger's "Authorize" button) working unchanged.
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) &&
                    context.Request.Cookies.TryGetValue(AuthCookieDefaults.CookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },

            // Signature/issuer/audience/lifetime are already valid by this point — this is
            // the one additional check standard JWT validation can't express on its own:
            // "has this specific token been explicitly logged out." Resolved from
            // RequestServices (not a constructor parameter) because JwtBearerEvents is
            // built once at startup, before per-request DI scopes exist.
            OnTokenValidated = context =>
            {
                var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var revocationStore = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationStore>();

                if (jti is not null && revocationStore.IsRevoked(jti))
                {
                    context.Fail("Token has been revoked.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// Fail fast instead of silently deploying a CORS policy that blocks every browser
// request — an empty allow-list outside Development is almost certainly a missing
// config value, not an intentional "block everyone".
if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must list at least one allowed origin outside the Development environment.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        // AllowCredentials is required so the browser sends/accepts the HttpOnly auth
        // cookie on cross-origin requests (the client and API run on different ports).
        // Only valid alongside a specific origin allow-list — never with AllowAnyOrigin().
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Server-side defense-in-depth against brute force: LoginFailed (the flowchart's lockout
// counter) is entirely client-side state, so anyone calling these endpoints directly
// (curl/Postman/script) bypasses it completely. Partitioned per client IP so one abusive
// caller can't exhaust the limit for everyone else; a fixed window keeps behavior simple
// and predictable to reason about and test.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimiterPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Not meaningful in Development, which this project always runs over plain HTTP (see
    // the SameSite/cookie note in Sprint 8) — HSTS only matters once real HTTPS is served.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Applies to every response, API-wide: tells the browser to trust the Content-Type header
// as-is instead of guessing (MIME-sniffing) from the response body. Cheap, standard
// hardening with no behavioral trade-off for a JSON API like this one.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    await next();
});

app.UseCors(ClientCorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Top-level statements generate an `internal` Program class by default. Making it
// public here lets AuthApp.Api.Tests spin up the real app via WebApplicationFactory<Program>
// for integration tests (e.g. proving [Authorize] is actually enforced end-to-end).
public partial class Program;
