using BackendApplication.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BackendApplication.Tests;

internal sealed class SqliteTestDb : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private SqliteTestDb(SqliteConnection connection, AppDbContext dbContext)
    {
        this.connection = connection;
        DbContext = dbContext;
    }

    public AppDbContext DbContext { get; }

    public static async Task<SqliteTestDb> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        return new SqliteTestDb(connection, dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
