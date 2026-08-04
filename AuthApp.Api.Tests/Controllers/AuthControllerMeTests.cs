using System.Security.Claims;
using AuthApp.Api.Controllers;
using AuthApp.Api.Models.Dtos;
using AuthApp.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthApp.Api.Tests.Controllers;

public class AuthControllerMeTests
{
    private static AuthController CreateControllerAsUser(string? username)
    {
        var controller = new AuthController(
            Mock.Of<IUserStore>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IJwtTokenGenerator>(),
            Mock.Of<ILogger<AuthController>>());

        var identity = username is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }

    [Fact]
    public void Me_returns_the_username_from_the_authenticated_principal()
    {
        var sut = CreateControllerAsUser("bob");

        var result = sut.Me();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UserInfoResponse>(okResult.Value);
        Assert.Equal("bob", response.Username);
    }

    [Fact]
    public void Me_returns_unauthorized_when_there_is_no_authenticated_name()
    {
        var sut = CreateControllerAsUser(null);

        var result = sut.Me();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}
