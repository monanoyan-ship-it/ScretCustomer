using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Data;

namespace SecretCustomer.Tests.Integration.Infrastructure;

/// <summary>
/// SQLite in-memory DbContext factory for integration tests.
/// SQLite, InMemory provider'dan farklı olarak query filter'ları ve
/// ilişkisel kısıtlamaları destekler.
/// </summary>
public static class SqliteDbContextFactory
{
    public static (ApplicationDbContext Context, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
