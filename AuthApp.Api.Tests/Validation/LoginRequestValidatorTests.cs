using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Validation;
using FluentValidation.TestHelper;

namespace AuthApp.Api.Tests.Validation;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_have_error_when_username_is_empty()
    {
        var result = _validator.TestValidate(new LoginRequest("", "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_have_error_when_password_is_empty()
    {
        var result = _validator.TestValidate(new LoginRequest("user", ""));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public void Should_treat_whitespace_or_null_username_as_empty(string? username)
    {
        var result = _validator.TestValidate(new LoginRequest(username!, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_have_error_when_username_is_shorter_than_3_characters()
    {
        var result = _validator.TestValidate(new LoginRequest("ab", "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters long.");
    }

    [Fact]
    public void Should_have_error_when_username_contains_spaces()
    {
        var result = _validator.TestValidate(new LoginRequest("bad user", "secret123"));

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
        var result = _validator.TestValidate(new LoginRequest(username, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, underscores, and hyphens.");
    }

    [Fact]
    public void Should_have_error_when_username_is_longer_than_32_characters()
    {
        var username = new string('a', 33);

        var result = _validator.TestValidate(new LoginRequest(username, "secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at most 32 characters long.");
    }

    [Fact]
    public void Should_have_error_when_password_is_shorter_than_8_characters()
    {
        var result = _validator.TestValidate(new LoginRequest("user", "1234567"));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void Should_have_error_when_password_is_longer_than_128_characters()
    {
        var password = new string('a', 129);

        var result = _validator.TestValidate(new LoginRequest("user", password));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at most 128 characters long.");
    }

    [Fact]
    public void Should_not_have_any_errors_when_request_is_valid()
    {
        var result = _validator.TestValidate(new LoginRequest("user", "secret123"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
