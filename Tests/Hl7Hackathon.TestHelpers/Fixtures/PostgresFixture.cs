using Microsoft.AspNetCore.Builder;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hl7Hackathon.TestHelpers.Fixtures;

/// <summary>A migrated and seeded PostgreSQL container. FHIR calls point at an unreachable address.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string PostgresImage = "postgres:17-alpine";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder(PostgresImage).Build();
    private WebApplication? _app;

    public IServiceProvider Services => _app?.Services
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _app = TestAppFactory.CreateMigrated(_postgresContainer.GetConnectionString(), "http://localhost:1/fhir");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }
}
