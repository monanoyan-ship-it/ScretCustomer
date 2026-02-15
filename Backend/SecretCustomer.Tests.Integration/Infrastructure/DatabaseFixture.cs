using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Data;

namespace SecretCustomer.Tests.Integration.Infrastructure;

/// <summary>
/// xUnit collection fixture for sharing SQLite database across tests in a collection.
/// Her test collection'ı kendi DB'sini alır.
/// </summary>
public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; }
    private readonly SqliteConnection _connection;

    public DatabaseFixture()
    {
        var (context, connection) = SqliteDbContextFactory.Create();
        Context = context;
        _connection = connection;

        SeedData();
    }

    private void SeedData()
    {
        // Seed a default admin user
        Context.Users.Add(new User
        {
            Id = 1,
            Username = "admin",
            Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            FirstName = "Admin",
            LastName = "User",
            RoleId = UserRoles.Ids.Admin,
            IsActive = true
        });

        // Seed a default customer
        Context.Customers.Add(new Customer
        {
            Id = 1,
            CompanyName = "Test Company",
            TaxNumber = "1234567890",
            IsActive = true
        });

        Context.SaveChanges();
    }

    /// <summary>
    /// Test sonrası temiz context almak için (aynı connection üzerinde)
    /// </summary>
    public ApplicationDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
