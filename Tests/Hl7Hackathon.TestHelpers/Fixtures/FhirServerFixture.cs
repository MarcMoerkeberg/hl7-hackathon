using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hl7Hackathon.TestHelpers.Fixtures;

/// <summary>PostgreSQL plus a throwaway HAPI FHIR server, so FHIR calls never touch a shared server.</summary>
public sealed class FhirServerFixture : IAsyncLifetime
{
    public const string HapiImage = "hapiproject/hapi:latest";
    private const ushort HapiPort = 8080;

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder(PostgresFixture.PostgresImage).Build();

    // HAPI is a Spring Boot app and regularly needs more than a minute to answer its first request.
    private readonly IContainer _hapiContainer = new ContainerBuilder(HapiImage)
        .WithPortBinding(HapiPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
            request => request.ForPort(HapiPort).ForPath("/fhir/metadata"),
            wait => wait.WithTimeout(TimeSpan.FromMinutes(4))))
        .Build();

    private WebApplication? _app;

    public IServiceProvider Services => _app?.Services
        ?? throw new InvalidOperationException("The fixture has not been initialized.");

    public string FhirBaseUrl => $"http://{_hapiContainer.Hostname}:{_hapiContainer.GetMappedPublicPort(HapiPort)}/fhir";

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgresContainer.StartAsync(), _hapiContainer.StartAsync());
        _app = TestAppFactory.CreateMigrated(_postgresContainer.GetConnectionString(), FhirBaseUrl);
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        await _hapiContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
