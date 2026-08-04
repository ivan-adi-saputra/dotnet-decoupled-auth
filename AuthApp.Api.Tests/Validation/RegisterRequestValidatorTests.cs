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
        var result = _validator.TestValidate(new RegisterRequest("", "secret"));

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
        var result = _validator.TestValidate(new RegisterRequest(username!, "secret"));

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Should_not_have_any_errors_when_request_is_valid()
    {
        var result = _validator.TestValidate(new RegisterRequest("user", "secret"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
