namespace AuthApp.Api.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(string username);
}
