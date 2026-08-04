using AuthApp.Api.ErrorHandling;
using AuthApp.Api.Filters;
using AuthApp.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string ClientCorsPolicy = "AuthAppClient";

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// FluentValidation (via ValidationFilter) is the single source of truth for request
// validation. Disable ASP.NET Core's automatic ModelState validation so a future
// DataAnnotation on a DTO can't silently introduce a second, differently-shaped
// error response alongside it.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(ClientCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
