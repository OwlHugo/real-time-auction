namespace RealTimeAuction.Application.FunctionalTests;

public static class TestDatabaseFactory
{
    public static async Task<ITestDatabase> CreateAsync()
    {
        // Requer Docker (Testcontainers sobe um PostgreSQL descartável).
        var database = new SqlTestcontainersTestDatabase();

        await database.InitialiseAsync();

        return database;
    }
}
