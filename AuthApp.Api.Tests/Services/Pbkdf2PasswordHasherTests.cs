using AuthApp.Api.Services;

namespace AuthApp.Api.Tests.Services;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_returns_true_for_the_correct_password()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.True(_hasher.Verify("Secret123", hash));
    }

    [Fact]
    public void Verify_returns_false_for_an_incorrect_password()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_uses_a_random_salt_so_the_same_password_hashes_differently_each_time()
    {
        var hash1 = _hasher.Hash("Secret123");
        var hash2 = _hasher.Hash("Secret123");

        Assert.NotEqual(hash1, hash2);
        // ...but both must still verify against the original password.
        Assert.True(_hasher.Verify("Secret123", hash1));
        Assert.True(_hasher.Verify("Secret123", hash2));
    }

    [Theory]
    [InlineData("not-a-valid-hash")]
    [InlineData("1.2")]
    [InlineData("abc.aGVsbG8=.aGVsbG8=")]
    [InlineData("100000.not-valid-base64!!!.aGVsbG8=")]
    public void Verify_returns_false_instead_of_throwing_for_a_malformed_stored_hash(string malformedHash)
    {
        Assert.False(_hasher.Verify("anything", malformedHash));
    }
}
