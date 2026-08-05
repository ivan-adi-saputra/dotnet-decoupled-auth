using AuthApp.Api.Controllers;
using AuthApp.Api.Models;
using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthApp.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserStore> _userStore = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(
            _userStore.Object,
            _passwordHasher.Object,
            _tokenGenerator.Object,
            Mock.Of<IWebHostEnvironment>(),
            Mock.Of<ILogger<AuthController>>());
    }

    [Fact]
    public void Register_returns_ok_when_the_username_is_new()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _userStore.Setup(s => s.TryAdd(It.IsAny<User>())).Returns(true);

        var result = _sut.Register(new RegisterRequest("bob", "secret"));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Registration successful. You can now log in.", response.Message);
    }

    [Fact]
    public void Register_returns_conflict_when_the_username_already_exists()
    {
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _userStore.Setup(s => s.TryAdd(It.IsAny<User>())).Returns(false);

        var result = _sut.Register(new RegisterRequest("bob", "secret"));

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Username 'bob' is already taken.", response.Message);
    }

    [Fact]
    public void Register_stores_the_hashed_password_never_the_plain_text_one()
    {
        _passwordHasher.Setup(h => h.Hash("plain-text")).Returns("hashed-value");
        User? capturedUser = null;
        _userStore.Setup(s => s.TryAdd(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(true);

        _sut.Register(new RegisterRequest("bob", "plain-text"));

        Assert.Equal("hashed-value", capturedUser!.PasswordHash);
        Assert.NotEqual("plain-text", capturedUser.PasswordHash);
    }
}
