using Microsoft.Extensions.Configuration;

namespace Hl7Hackathon.AppHost.Extensions;

public static class DistributedApplicationBuilderExtensions
{
    private const string DatabaseName = "hl7hackathon";
    private const string LocalFhirServerEnabledKey = "LocalFhirServer:Enabled";
    private const int LocalFhirServerPort = 8080;

    /// <summary>
    /// Adds PostgreSQL (with pgAdmin), the web app and, when <c>LocalFhirServer:Enabled</c> is true, a local HAPI FHIR
    /// server that the web app is pointed at instead of the one in its own configuration.
    /// </summary>
    public static void AddHl7Hackathon(this IDistributedApplicationBuilder builder)
    {
        var database = builder.AddDatabase();

        var web = builder.AddProject<Projects.Hl7Hackathon_Web>("web")
            .WithEnvironment("DbConnectionString", database)
            .WaitFor(database)
            .WithExternalHttpEndpoints();

        if (builder.Configuration.GetValue<bool>(LocalFhirServerEnabledKey))
        {
            var fhirServer = builder.AddLocalFhirServer();

            web.WithEnvironment("FhirServer__Name", "LocalHapi")
                .WithEnvironment("FhirServer__BaseUrl", ReferenceExpression.Create($"{fhirServer.GetEndpoint("http")}/fhir/"))
                .WaitFor(fhirServer);
        }
    }

    /// <summary>
    /// Adds a persistent PostgreSQL container with a data volume, so restarts are fast and data survives them.
    /// </summary>
    private static IResourceBuilder<PostgresDatabaseResource> AddDatabase(this IDistributedApplicationBuilder builder)
    {
        return builder.AddPostgres("postgres")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent))
            .AddDatabase(DatabaseName);
    }

    /// <summary>
    /// Adds a persistent HAPI FHIR server on a fixed host port, reachable at http://localhost:8080/fhir.
    /// </summary>
    /// <remarks>HAPI does not know the hackathon IG profiles, so it accepts resources the real server may reject.</remarks>
    private static IResourceBuilder<ContainerResource> AddLocalFhirServer(this IDistributedApplicationBuilder builder)
    {
        return builder.AddContainer("hapi-fhir", "hapiproject/hapi", "latest")
            .WithHttpEndpoint(port: LocalFhirServerPort, targetPort: 8080, name: "http")
            .WithHttpHealthCheck("/fhir/metadata")
            .WithLifetime(ContainerLifetime.Persistent);
    }
}
