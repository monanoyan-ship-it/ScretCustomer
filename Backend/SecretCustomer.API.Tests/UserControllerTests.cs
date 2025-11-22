using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SecretCustomer.API.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "test"),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}

public class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IUserService> _mockUserService;

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockUserService = new Mock<IUserService>();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove the actual IUserService registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IUserService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add the mock service
                services.AddScoped<IUserService>(_ => _mockUserService.Object);

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Evaluator,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserService
            .Setup(service => service.GetByIdAsync(userId))
            .ReturnsAsync(userDto);

        var client = _factory.CreateClient();

        // Note: In a real scenario, you would need to add authentication token
        // Act
        var response = await client.GetAsync($"/api/User/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NonExistingUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserService
            .Setup(service => service.GetByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/User/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Evaluator,
            IsActive = true
        };

        var createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            Username = createDto.Username,
            Email = createDto.Email,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Role = createDto.Role,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserService
            .Setup(service => service.CreateAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(createdUser);

        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/User", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
