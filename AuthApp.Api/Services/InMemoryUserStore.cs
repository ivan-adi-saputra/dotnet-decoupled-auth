using System.Collections.Concurrent;
using AuthApp.Api.Models;

namespace AuthApp.Api.Services;

/// <summary>
/// In-memory representation of the `users` data store from the register/login flowcharts.
/// Registered as a singleton so data survives across requests for the lifetime of the app.
/// </summary>
public class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public bool Exists(string username) => _users.ContainsKey(username);

    public bool TryAdd(User user) => _users.TryAdd(user.Username, user);

    public User? FindByUsername(string username) =>
        _users.TryGetValue(username, out var user) ? user : null;
}
