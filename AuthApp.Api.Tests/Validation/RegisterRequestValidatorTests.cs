using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Validation;
using FluentValidation.TestHelper;

namespace AuthApp.Api.Tests.Validation;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_have_error_when_username_is_empty()
    {
        var result = _validator.TestValidate(new RegisterRequest("", "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_have_error_when_password_is_empty()
    {
        var result = _validator.TestValidate(new RegisterRequest("user", ""));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_treat_whitespace_or_null_username_as_empty(string? username)
    {
        var result = _validator.TestValidate(new RegisterRequest(username!, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_have_error_when_username_is_shorter_than_3_characters()
    {
        var result = _validator.TestValidate(new RegisterRequest("ab", "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters long.");
    }

    [Fact]
    public void Should_have_error_when_username_contains_spaces()
    {
        var result = _validator.TestValidate(new RegisterRequest("bad user", "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, underscores, and hyphens.");
    }

    [Theory]
    [InlineData("<svg/onload=alert(1)>")]
    [InlineData("bad'user")]
    [InlineData("bad\"user")]
    [InlineData("bad<user>")]
    public void Should_have_error_when_username_contains_html_metacharacters(string username)
    {
        // Usernames get echoed verbatim into messages that reach UI surfaces which
        // interpret HTML (e.g. SweetAlert2 toasts) — proven live that an unrestricted
        // charset let "<svg/onload=alert(1)>" execute as real markup. Rejecting these
        // characters at the source is the primary defense against that.
        var result = _validator.TestValidate(new RegisterRequest(username, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, underscores, and hyphens.");
    }

    [Fact]
    public void Should_have_error_when_username_is_longer_than_32_characters()
    {
        var username = new string('a', 33);

        var result = _validator.TestValidate(new RegisterRequest(username, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at most 32 characters long.");
    }

    [Fact]
    public void Should_have_error_when_password_is_shorter_than_8_characters()
    {
        var result = _validator.TestValidate(new RegisterRequest("user", "1234567"));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void Should_have_error_when_password_is_longer_than_128_characters()
    {
        var password = new string('a', 129);

        var result = _validator.TestValidate(new RegisterRequest("user", password));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at most 128 characters long.");
    }

    [Theory]
    [InlineData("password123")]
    [InlineData("qwerty123")]
    [InlineData("iloveyou1")]
    [InlineData("PaSsWoRd1")]
    public void Should_have_error_when_password_is_a_common_password(string password)
    {
        var result = _validator.TestValidate(new RegisterRequest("user", password));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("This password is too common. Please choose a different one.");
    }

    [Fact]
    public void Should_not_have_any_errors_when_request_is_valid()
    {
        var result = _validator.TestValidate(new RegisterRequest("user", "secret123"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_not_have_any_errors_when_username_contains_underscore_and_hyphen()
    {
        var result = _validator.TestValidate(new RegisterRequest("user_name-42", "secret123"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
