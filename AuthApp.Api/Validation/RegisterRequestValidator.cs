using AuthApp.Api.Models.Dtos;
using FluentValidation;

namespace AuthApp.Api.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
