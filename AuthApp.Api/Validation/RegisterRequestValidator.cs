using System.Text.RegularExpressions;
using AuthApp.Api.Models.Dtos;
using FluentValidation;

namespace AuthApp.Api.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>
    /// Letters, digits, underscore, and hyphen only. Deliberately restrictive: usernames
    /// get echoed verbatim into messages (e.g. "Username 'x' is already taken.") that reach
    /// UI surfaces such as SweetAlert2 toasts, which interpret HTML in some parameters — a
    /// permissive charset here previously allowed HTML injection (proven live with a
    /// username of "&lt;svg/onload=alert(1)&gt;"). Rejecting the dangerous characters at
    /// the source is the real fix; the toast rendering was also hardened separately, but
    /// this is the primary defense.
    /// </summary>
    public static readonly Regex UsernameCharset = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(32).WithMessage("Username must be at most 32 characters long.")
            .Matches(UsernameCharset).WithMessage("Username can only contain letters, numbers, underscores, and hyphens.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128).WithMessage("Password must be at most 128 characters long.")
            // Checked only at Register (where a password is chosen), not Login (where one
            // is just being verified) — per NIST 800-63B guidance to screen new passwords
            // against known-common ones, not to retroactively reject existing credentials.
            .Must(password => !CommonPasswords.IsCommon(password))
            .WithMessage("This password is too common. Please choose a different one.");
    }
}
