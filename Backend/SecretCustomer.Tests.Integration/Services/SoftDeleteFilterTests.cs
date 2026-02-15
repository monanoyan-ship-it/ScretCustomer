using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Tests.Integration.Infrastructure;

namespace SecretCustomer.Tests.Integration.Services;

/// <summary>
/// SQLite ile soft delete query filter testleri.
/// InMemory provider query filter desteklemez, SQLite destekler.
/// </summary>
[Collection("Database")]
public class SoftDeleteFilterTests
{
    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedUsers()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            context.Users.Add(new User
            {
                Username = "active",
                Email = "a@test.com",
                PasswordHash = "hash",
                FirstName = "Active",
                LastName = "User",
                RoleId = UserRoles.Ids.Admin,
                IsActive = true
            });
            context.Users.Add(new User
            {
                Username = "deleted",
                Email = "d@test.com",
                PasswordHash = "hash",
                FirstName = "Deleted",
                LastName = "User",
                RoleId = UserRoles.Ids.Admin,
                IsDeleted = true
            });
            await context.SaveChangesAsync();

            // Act
            var users = await context.Users.ToListAsync();
            var allUsers = await context.Users.IgnoreQueryFilters().ToListAsync();

            // Assert
            users.Should().HaveCount(1);
            users[0].Username.Should().Be("active");
            allUsers.Should().HaveCount(2);
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedAnnouncements()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            context.Announcements.Add(new Announcement
            {
                Title = "Active",
                Content = "Content",
                IsActive = true
            });
            context.Announcements.Add(new Announcement
            {
                Title = "Deleted",
                Content = "Content",
                IsDeleted = true
            });
            await context.SaveChangesAsync();

            // Act
            var announcements = await context.Announcements.ToListAsync();

            // Assert
            announcements.Should().HaveCount(1);
            announcements[0].Title.Should().Be("Active");
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedProjects()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            // Need a customer and checklist first
            var customer = new Customer
            {
                CompanyName = "Test",
                TaxNumber = "123",
                IsActive = true
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var checklist = new Checklist
            {
                Name = "Test Checklist",
                Description = "Test"
            };
            context.Checklists.Add(checklist);
            await context.SaveChangesAsync();

            context.Projects.Add(new Project
            {
                Name = "Active Project",
                CustomerId = customer.Id,
                ChecklistId = checklist.Id,
                ProjectTypeId = 1
            });
            context.Projects.Add(new Project
            {
                Name = "Deleted Project",
                CustomerId = customer.Id,
                ChecklistId = checklist.Id,
                ProjectTypeId = 1,
                IsDeleted = true
            });
            await context.SaveChangesAsync();

            // Act
            var projects = await context.Projects.ToListAsync();

            // Assert
            projects.Should().HaveCount(1);
            projects[0].Name.Should().Be("Active Project");
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task QueryFilter_ExcludesSoftDeletedCustomers()
    {
        // Arrange
        var (context, connection) = SqliteDbContextFactory.Create();
        try
        {
            context.Customers.Add(new Customer
            {
                CompanyName = "Active Co",
                TaxNumber = "111",
                IsActive = true
            });
            context.Customers.Add(new Customer
            {
                CompanyName = "Deleted Co",
                TaxNumber = "222",
                IsDeleted = true
            });
            await context.SaveChangesAsync();

            // Act
            var customers = await context.Customers.ToListAsync();
            var all = await context.Customers.IgnoreQueryFilters().ToListAsync();

            // Assert
            customers.Should().HaveCount(1);
            all.Should().HaveCount(2);
        }
        finally
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}
