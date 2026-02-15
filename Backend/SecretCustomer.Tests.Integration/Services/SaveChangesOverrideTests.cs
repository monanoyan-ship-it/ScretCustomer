using FluentAssertions;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Tests.Integration.Infrastructure;

namespace SecretCustomer.Tests.Integration.Services;

/// <summary>
/// SaveChangesAsync override testleri.
/// DbContext, Added entity'lerde CreatedAt, Modified entity'lerde UpdatedAt set eder.
/// </summary>
public class SaveChangesOverrideTests
{
    [Fact]
    public async Task SaveChangesAsync_SetsCreatedAtForNewEntities()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var user = new User
            {
                Username = "test",
                Email = "test@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                RoleId = UserRoles.Ids.Admin
            };

            // Act
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Assert - CreatedAt should be set by SaveChangesAsync override
            user.CreatedAt.Should().BeAfter(before);
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SaveChangesAsync_SetsUpdatedAtForModifiedEntities()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            var user = new User
            {
                Username = "test",
                Email = "test@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                RoleId = UserRoles.Ids.Admin
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            user.UpdatedAt.Should().BeNull();

            // Act
            user.FirstName = "Updated";
            await context.SaveChangesAsync();

            // Assert
            user.UpdatedAt.Should().NotBeNull();
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}
