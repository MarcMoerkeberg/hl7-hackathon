using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hl7Hackathon.TestHelpers;

/// <summary>
/// Builds a web app with the same backend registrations as <c>Program.cs</c>, pointed at test containers,
/// without starting a server. Logging, telemetry and the Blazor frontend are left out.
/// </summary>
public static class TestAppFactory
{
    public const string TestServerName = "Test";

    public static WebApplication CreateMigrated(string connectionString, string fhirBaseUrl)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [nameof(AppSettings.DbConnectionString)] = connectionString,
            [nameof(AppSettings.SeedDatabase)] = "true",
            [$"{nameof(AppSettings.FhirServer)}:{nameof(ServerConnection.Name)}"] = TestServerName,
            [$"{nameof(AppSettings.FhirServer)}:{nameof(ServerConnection.BaseUrl)}"] = fhirBaseUrl,
            [$"{nameof(AppSettings.FhirServer)}:{nameof(ServerConnection.RequestTimeout)}"] = "00:00:30"
        });

        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        AppSettings appSettings = builder.AddAppSettings();
        builder.Services.AddDatabase(appSettings);
        builder.Services.AddFhirHttpClient(appSettings);
        builder.Services.AddDependencyInjection();

        var app = builder.Build();
        app.RunMigrations();

        return app;
    }
}
