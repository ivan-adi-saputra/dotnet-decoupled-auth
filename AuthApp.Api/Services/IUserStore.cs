using AuthApp.Api.Models;

namespace AuthApp.Api.Services;

public interface IUserStore
{
    bool Exists(string username);

    /// <summary>
    /// Atomically inserts the user if the username is not already taken.
    /// Returns false instead of overwriting when the username already exists.
    /// </summary>
    bool TryAdd(User user);
    User? FindByUsername(string username);
}
