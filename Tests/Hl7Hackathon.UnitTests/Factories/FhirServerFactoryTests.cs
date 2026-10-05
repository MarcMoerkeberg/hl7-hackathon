using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models.CapabilityStatements;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class FhirServerFactoryTests
{
    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
    private readonly FhirServerFactory _factory;

    public FhirServerFactoryTests()
    {
        var appSettings = new AppSettings
        {
            DbConnectionString = "Host=localhost",
            FhirServer = new ServerConnection { Name = "HapiPublic", BaseUrl = "https://hapi.fhir.org/baseR4" }
        };

        _factory = new FhirServerFactory(Options.Create(appSettings), _fhirService, NullLogger<FhirServerFactory>.Instance);
    }

    [Fact]
    public async Task GetFhirServerInfoAsync_ReturnsTheMappedCapabilityStatement()
    {
        // Arrange
        var capabilityStatement = new CapabilityStatement
        {
            Status = "active",
            FhirVersion = "4.0.1",
            Software = new CapabilityStatementSoftware { Name = "HAPI FHIR Server", Version = "8.0.0" },
            Rest =
            [
                new CapabilityStatementRest
                {
                    Mode = "server",
                    Resource = [new CapabilityStatementResource { Type = "Patient" }, new CapabilityStatementResource { Type = "EpisodeOfCare" }]
                }
            ]
        };
        _fhirService.GetCapabilityStatementAsync(Arg.Any<CancellationToken>()).Returns(capabilityStatement);

        // Act
        var result = await _factory.GetFhirServerInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("HapiPublic", result.Value.Name);
        Assert.Equal("https://hapi.fhir.org/baseR4/", result.Value.BaseUrl);
        Assert.Equal("HAPI FHIR Server", result.Value.SoftwareName);
        Assert.Equal("4.0.1", result.Value.FhirVersion);
        Assert.Equal(["EpisodeOfCare", "Patient"], result.Value.SupportedResourceTypes);
    }

    [Fact]
    public async Task GetFhirServerInfoAsync_ReturnsTheServiceError_WhenTheServerFails()
    {
        // Arrange
        var error = Error.Unavailable("GET metadata could not reach the FHIR server");
        _fhirService.GetCapabilityStatementAsync(Arg.Any<CancellationToken>()).Returns(error);

        // Act
        var result = await _factory.GetFhirServerInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetFhirServerInfoAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _fhirService.GetCapabilityStatementAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("resilience timeout"));

        // Act
        var result = await _factory.GetFhirServerInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
        Assert.Contains("HapiPublic", result.Error?.Message);
    }
}
