using AuthApp.Api.Controllers;
using AuthApp.Api.Models;
using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthApp.Api.Tests.Controllers;

public class AuthControllerLoginTests
{
    private readonly Mock<IUserStore> _userStore = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly AuthController _sut;

    public AuthControllerLoginTests()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns("Development");

        _sut = new AuthController(
            _userStore.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            Mock.Of<IJwtTokenValidator>(),
            Mock.Of<ITokenRevocationStore>(),
            environment.Object,
            Mock.Of<ILogger<AuthController>>())
        {
            // Login() now sets a cookie on success, which needs a real HttpContext
            // (ControllerContext.HttpContext is null by default when constructing a
            // controller directly instead of through the MVC pipeline).
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public void Login_returns_ok_with_a_token_when_credentials_are_valid()
    {
        var user = new User { Username = "bob", PasswordHash = "hashed" };
        _userStore.Setup(s => s.FindByUsername("bob")).Returns(user);
        _passwordHasher.Setup(h => h.Verify("secret", "hashed")).Returns(true);
        _tokenGenerator.Setup(t => t.GenerateToken("bob")).Returns("fake-jwt-token");

        var result = _sut.Login(new LoginRequest("bob", "secret"));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("fake-jwt-token", response.Token);
    }

    [Fact]
    public void Login_does_not_generate_a_token_when_the_user_does_not_exist()
    {
        _userStore.Setup(s => s.FindByUsername(It.IsAny<string>())).Returns((User?)null);

        var result = _sut.Login(new LoginRequest("ghost", "secret"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(unauthorized.Value);
        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.Equal("Username not found.", response.Message);
        _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Login_does_not_generate_a_token_when_the_password_is_wrong()
    {
        var user = new User { Username = "bob", PasswordHash = "hashed" };
        _userStore.Setup(s => s.FindByUsername("bob")).Returns(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), "hashed")).Returns(false);

        var result = _sut.Login(new LoginRequest("bob", "wrong-password"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<LoginResponse>(unauthorized.Value);
        Assert.False(response.Success);
        Assert.Null(response.Token);
        Assert.Equal("Incorrect password.", response.Message);
        _tokenGenerator.Verify(t => t.GenerateToken(It.IsAny<string>()), Times.Never);
    }
}
