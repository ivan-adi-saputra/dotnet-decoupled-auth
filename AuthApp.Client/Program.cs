using AuthApp.Client;
using AuthApp.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

builder.Services.AddTransient<CredentialsIncludedHandler>();
builder.Services
    .AddHttpClient<AuthApiService>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<CredentialsIncludedHandler>();

builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<LoginAttemptTracker>();
builder.Services.AddScoped<NotificationService>();

await builder.Build().RunAsync();
