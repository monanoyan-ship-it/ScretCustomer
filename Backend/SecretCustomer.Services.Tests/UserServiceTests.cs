using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SecretCustomer.Core.DTOs.User;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Services.Services;

namespace SecretCustomer.Services.Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();

        // In-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _userService = new UserService(_mockUserRepository.Object, _mockAuditLogService.Object, _dbContext);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.QualitySpecialist,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ReturnsNull()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidData_ReturnsCreatedUser()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.QualitySpecialist,
            IsActive = true
        };

        _mockUserRepository
            .Setup(repo => repo.ExistsByUsernameAsync(createDto.Username))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(repo => repo.ExistsByEmailAsync(createDto.Email))
            .ReturnsAsync(false);

        _mockUserRepository
            .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        var result = await _userService.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be(createDto.Username);
        result.Email.Should().Be(createDto.Email);
        result.FirstName.Should().Be(createDto.FirstName);
        result.LastName.Should().Be(createDto.LastName);
        result.Role.Should().Be(createDto.Role);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Username = "existinguser",
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.QualitySpecialist
        };

        _mockUserRepository
            .Setup(repo => repo.ExistsByUsernameAsync(createDto.Username))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _userService.CreateAsync(createDto)
        );
    }

    [Fact]
    public async Task DeleteAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository
            .Setup(repo => repo.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _userService.DeleteAsync(userId)
        );
    }
}
