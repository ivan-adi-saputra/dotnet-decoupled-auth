using AuthApp.Api.Models;
using AuthApp.Api.Services;

namespace AuthApp.Api.Tests.Services;

public class InMemoryUserStoreTests
{
    [Fact]
    public void TryAdd_returns_true_for_a_new_username()
    {
        var store = new InMemoryUserStore();

        Assert.True(store.TryAdd(new User { Username = "alice", PasswordHash = "hash" }));
        Assert.True(store.Exists("alice"));
    }

    [Fact]
    public void TryAdd_returns_false_for_a_duplicate_username()
    {
        var store = new InMemoryUserStore();
        store.TryAdd(new User { Username = "alice", PasswordHash = "hash1" });

        Assert.False(store.TryAdd(new User { Username = "alice", PasswordHash = "hash2" }));
    }

    [Fact]
    public void TryAdd_is_case_insensitive()
    {
        var store = new InMemoryUserStore();
        store.TryAdd(new User { Username = "alice", PasswordHash = "hash1" });

        Assert.False(store.TryAdd(new User { Username = "ALICE", PasswordHash = "hash2" }));
    }

    [Fact]
    public void TryAdd_never_overwrites_the_original_user_on_a_duplicate_attempt()
    {
        var store = new InMemoryUserStore();
        store.TryAdd(new User { Username = "alice", PasswordHash = "original" });

        store.TryAdd(new User { Username = "alice", PasswordHash = "attempted-overwrite" });

        Assert.Equal("original", store.FindByUsername("alice")!.PasswordHash);
    }

    [Fact]
    public async Task TryAdd_is_atomic_under_concurrent_calls_with_the_same_username()
    {
        // Regression test for the race condition fixed in Sprint 1: with 50 requests
        // racing to register the same username, exactly one must win.
        var store = new InMemoryUserStore();

        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => store.TryAdd(new User { Username = "race", PasswordHash = $"hash-{i}" })));
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(succeeded => succeeded));
    }
}
