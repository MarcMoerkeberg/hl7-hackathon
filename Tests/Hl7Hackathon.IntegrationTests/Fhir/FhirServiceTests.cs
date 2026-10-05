using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Fhir;

/// <summary>
/// Runs against a throwaway HAPI server. HAPI does not know the IG profiles, so these prove the REST flow and our JSON,
/// not that the resources pass the hackathon server's validation.
/// </summary>
[Collection(FhirServerTestsCollection.Name)]
[Trait("Category", "FhirServer")]
public class FhirServiceTests
{
    private readonly FhirServerFixture _fixture;

    public FhirServiceTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetFhirServerInfoAsync_ReturnsTheServerMetadata()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFhirServerFactory>();

        // Act
        var result = await factory.GetFhirServerInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("4.0.1", result.Value.FhirVersion);
        Assert.Contains("EpisodeOfCare", result.Value.SupportedResourceTypes);
    }

    [Fact]
    public async Task CreateResourceAsync_ThenGetResourceByIdAsync_ReturnsTheSamePatient_AndLogsTheExchange()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var fhirExchangeService = scope.ServiceProvider.GetRequiredService<IFhirExchangeService>();

        // Act
        var created = await fhirService.CreateResourceAsync(TestFhirResources.TelemedicinePatient(CreateCpr()), cancellationToken: TestContext.Current.CancellationToken);
        var read = await fhirService.GetResourceByIdAsync<Patient>(created.Value.Id!, TestContext.Current.CancellationToken);
        var exchanges = await fhirExchangeService.GetRecentFhirExchangesAsync(10, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(read.IsSuccess, read.Error?.Message);
        Assert.Equal("Berggren", read.Value.Name!.Single().Family);
        Assert.Same(AdministrativeGender.Female, read.Value.Gender);
        Assert.Contains(exchanges, exchange => exchange.Method == "POST" && exchange.StatusCode == 201);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsTheExistingPatient_WhenIfNoneExistMatches()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var cpr = CreateCpr();
        var ifNoneExist = $"identifier={TestFhirResources.CprSystem}|{cpr}";

        // Act
        var first = await fhirService.CreateResourceAsync(TestFhirResources.TelemedicinePatient(cpr), ifNoneExist, TestContext.Current.CancellationToken);
        var second = await fhirService.CreateResourceAsync(TestFhirResources.TelemedicinePatient(cpr), ifNoneExist, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(first.Value.Id, second.Value.Id);
    }

    [Fact]
    public async Task SearchResourcesAsync_FindsThePatientByIdentifier()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var cpr = CreateCpr();
        await fhirService.CreateResourceAsync(TestFhirResources.TelemedicinePatient(cpr), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var bundle = await fhirService.SearchResourcesAsync<Patient>(
            [("identifier", $"{TestFhirResources.CprSystem}|{cpr}")],
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(bundle.IsSuccess, bundle.Error?.Message);
        Assert.IsType<Patient>(Assert.Single(bundle.Value.Entry!).Resource);
    }

    [Fact]
    public async Task PostTransactionAsync_CreatesPatientAndEpisodeOfCare_AndResolvesTheUrnReference()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var patientUrn = $"urn:uuid:{Guid.NewGuid()}";
        var transaction = new Bundle
        {
            Type = BundleType.Transaction,
            Entry =
            [
                new BundleEntry
                {
                    FullUrl = patientUrn,
                    Resource = TestFhirResources.TelemedicinePatient(CreateCpr()),
                    Request = new BundleEntryRequest { Method = HttpVerb.Post, Url = "Patient" }
                },
                new BundleEntry
                {
                    Resource = TestFhirResources.TelemedicineEpisodeOfCare(patientReference: patientUrn),
                    Request = new BundleEntryRequest { Method = HttpVerb.Post, Url = "EpisodeOfCare" }
                }
            ]
        };

        // Act
        var response = await fhirService.PostTransactionAsync(transaction, TestContext.Current.CancellationToken);
        var episodeOfCareId = ReadIdFromLocation(response.Value.Entry![1]);
        var episodeOfCare = await fhirService.GetResourceByIdAsync<EpisodeOfCare>(episodeOfCareId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(BundleType.TransactionResponse, response.Value.Type);
        Assert.All(response.Value.Entry!, entry => Assert.StartsWith("201", entry.Response!.Status));
        Assert.Equal($"Patient/{ReadIdFromLocation(response.Value.Entry![0])}", episodeOfCare.Value.Patient.Reference);
    }

    [Fact]
    public async Task GetNextPageAsync_ReturnsTheNextPage_UntilTheLastOne()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var patient = await fhirService.CreateResourceAsync(TestFhirResources.TelemedicinePatient(CreateCpr()), cancellationToken: TestContext.Current.CancellationToken);
        var patientReference = $"Patient/{patient.Value.Id}";
        await fhirService.CreateResourceAsync(TestFhirResources.SleepObservation(patientReference), cancellationToken: TestContext.Current.CancellationToken);
        await fhirService.CreateResourceAsync(TestFhirResources.PhysicalActivityObservation(patientReference), cancellationToken: TestContext.Current.CancellationToken);
        var firstPage = await fhirService.SearchResourcesAsync<Observation>(
            [("subject", patientReference), ("_count", "1")],
            TestContext.Current.CancellationToken);

        // Act
        var secondPage = await fhirService.GetNextPageAsync(firstPage.Value, TestContext.Current.CancellationToken);
        var thirdPage = await fhirService.GetNextPageAsync(secondPage.Value!, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, firstPage.Value.Total);
        Assert.Single(secondPage.Value!.Entry!);
        Assert.Null(thirdPage.Value);
    }

    // A fresh identifier per test, so tests sharing the container never find each other's patients.
    private static string CreateCpr() => Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();

    private static string ReadIdFromLocation(BundleEntry entry) => entry.Response!.Location!.Split('/')[1];
}
