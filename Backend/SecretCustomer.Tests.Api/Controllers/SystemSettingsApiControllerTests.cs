using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecretCustomer.Core.DTOs.SystemSetting;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Data;
using SecretCustomer.Tests.Api.Helpers;
using SecretCustomer.Tests.Api.Infrastructure;

namespace SecretCustomer.Tests.Api.Controllers;

public class SystemSettingsApiControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public SystemSettingsApiControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void AuthenticateAsAdmin()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestAuthHelper.AdminToken());
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!await context.Users.AnyAsync(u => u.Id == 1))
        {
            context.Users.Add(new User
            {
                Id = 1,
                Username = "admin",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
                FirstName = "Admin",
                LastName = "User",
                RoleId = UserRoles.Ids.Admin,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
    }

    #region Authentication Tests

    [Fact]
    public async Task GetAll_WithoutAuth_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/system-settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task GetAll_WithAuth_Returns200()
    {
        // Arrange
        await SeedDataAsync();
        AuthenticateAsAdmin();

        // Act
        var response = await _client.GetAsync("/api/system-settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetByKey_NotFound_Returns404()
    {
        // Arrange
        await SeedDataAsync();
        AuthenticateAsAdmin();

        // Act
        var response = await _client.GetAsync("/api/system-settings/NonExistentKey");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithValidData_Returns200()
    {
        // Arrange
        await SeedDataAsync();
        AuthenticateAsAdmin();

        var dto = new CreateSystemSettingDto
        {
            Key = "TestKey_" + Guid.NewGuid().ToString("N")[..8],
            Value = "TestValue",
            Category = "General"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/system-settings", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        await SeedDataAsync();
        AuthenticateAsAdmin();

        // Act
        var response = await _client.DeleteAsync("/api/system-settings/NonExistentKey");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
