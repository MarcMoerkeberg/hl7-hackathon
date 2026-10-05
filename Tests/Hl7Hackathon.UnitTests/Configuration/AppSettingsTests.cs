using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hl7Hackathon.UnitTests.Configuration;

[Trait("Category", "Unit")]
public class AppSettingsTests
{
    [Fact]
    public void AddAppSettings_BindsTheConfiguration_WhenItIsValid()
    {
        // Arrange
        var options = CreateOptions(CreateConfiguration());

        // Act
        var appSettings = options.Value;

        // Assert
        Assert.Equal("HapiPublic", appSettings.FhirServer.Name);
        Assert.True(appSettings.SeedDatabase);
    }

    [Theory]
    [InlineData("https://hapi.fhir.org/baseR4")]
    [InlineData("https://hapi.fhir.org/baseR4/")]
    public void AddAppSettings_AddsTrailingSlashToBaseUrl_WhetherOrNotItIsConfigured(string configuredBaseUrl)
    {
        // Arrange
        var options = CreateOptions(CreateConfiguration(baseUrl: configuredBaseUrl));

        // Act
        var appSettings = options.Value;

        // Assert
        Assert.Equal("https://hapi.fhir.org/baseR4/", appSettings.FhirServer.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(60), appSettings.FhirServer.RequestTimeout);
    }

    [Fact]
    public void AddAppSettings_FailsValidation_WhenDbConnectionStringIsEmpty()
    {
        // Arrange
        var options = CreateOptions(CreateConfiguration(dbConnectionString: ""));

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains(nameof(AppSettings.DbConnectionString), exception.Message);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("hapi.fhir.org/baseR4")]
    public void AddAppSettings_FailsValidation_WhenFhirServerBaseUrlIsInvalid(string baseUrl)
    {
        // Arrange
        var options = CreateOptions(CreateConfiguration(baseUrl: baseUrl));

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() => options.Value);
        Assert.Contains(nameof(ServerConnection.BaseUrl), exception.Message);
    }

    private static Dictionary<string, string?> CreateConfiguration(
        string dbConnectionString = "Host=localhost;Database=hl7hackathon",
        string baseUrl = "https://hapi.fhir.org/baseR4") => new()
    {
        ["DbConnectionString"] = dbConnectionString,
        ["SeedDatabase"] = "true",
        ["FhirServer:Name"] = "HapiPublic",
        ["FhirServer:BaseUrl"] = baseUrl
    };

    private static IOptions<AppSettings> CreateOptions(Dictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.AddAppSettings();

        return builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AppSettings>>();
    }
}
