using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using RealTimeAuction.Infrastructure.Data;
using Respawn;
using Testcontainers.PostgreSql;

namespace RealTimeAuction.Application.FunctionalTests;

public class SqlTestcontainersTestDatabase : ITestDatabase
{
    private readonly PostgreSqlContainer _container;
    private DbConnection _connection = null!;
    private string _connectionString = null!;
    private Respawner _respawner = null!;

    public SqlTestcontainersTestDatabase()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine").WithAutoRemove(true).Build();
    }

    public async Task InitialiseAsync()
    {
        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();

        _connection = new NpgsqlConnection(_connectionString);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ApplicationDbContext(options);

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await _connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions { DbAdapter = DbAdapter.Postgres });
    }

    public DbConnection GetConnection() => _connection;

    public string GetConnectionString() => _connectionString;

    public async Task ResetAsync() => await _respawner.ResetAsync(_connection);

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
