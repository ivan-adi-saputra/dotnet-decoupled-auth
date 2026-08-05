using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace AuthApp.Api.Services;

public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly TokenValidationParameters _validationParameters;

    // Takes the exact same TokenValidationParameters instance JwtBearer uses (registered
    // once in Program.cs), so this can never validate against different rules than the
    // middleware that protects every other endpoint.
    public JwtTokenValidator(TokenValidationParameters validationParameters)
    {
        _validationParameters = validationParameters;
    }

    public bool TryValidate(string token, out string jti, out DateTimeOffset expiresAt)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, _validationParameters, out var validatedToken);
            var jtiClaim = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(jtiClaim))
            {
                jti = string.Empty;
                expiresAt = default;
                return false;
            }

            jti = jtiClaim;
            expiresAt = new DateTimeOffset(validatedToken.ValidTo, TimeSpan.Zero);
            return true;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            // Covers every "this isn't a token we should trust" case in one place: expired,
            // bad signature, wrong issuer/audience, or not even a well-formed JWT string.
            jti = string.Empty;
            expiresAt = default;
            return false;
        }
    }
}
