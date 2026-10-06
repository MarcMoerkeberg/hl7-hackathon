using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Fhir;

/// <summary>
/// Runs the whole create flow against a throwaway HAPI server: a seeded patient, the transaction, and the episode
/// of care read back. HAPI does not know the IG profiles, so this proves the flow, not profile validity.
/// </summary>
[Collection(FhirServerTestsCollection.Name)]
[Trait("Category", "FhirServer")]
public class EpisodeOfCareFactoryTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));

    private readonly FhirServerFixture _fixture;

    public EpisodeOfCareFactoryTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_PutsThePatientAndTheEpisodeOfCareOnTheServer()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IEpisodeOfCareFactory>();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var patientId = await GetSeededPatientIdAsync(scope.ServiceProvider, "2512489996");

        // Act
        var result = await factory.CreateEpisodeOfCareAsync(
            CreateRequest(patientId),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal("2026-10-06T00:00:00+02:00", result.Value.Start);
        Assert.StartsWith("Patient/", result.Value.PatientReference);

        var read = await fhirService.GetResourceByIdAsync<EpisodeOfCare>(result.Value.Id, TestContext.Current.CancellationToken);
        Assert.True(read.IsSuccess, read.Error?.Message);
        Assert.Equal(result.Value.PatientReference, read.Value.Patient.Reference);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReusesThePatient_WhenTheyAreAlreadyOnTheServer()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IEpisodeOfCareFactory>();
        var patientId = await GetSeededPatientIdAsync(scope.ServiceProvider, "1203049991");

        // Act
        var first = await factory.CreateEpisodeOfCareAsync(CreateRequest(patientId), TestContext.Current.CancellationToken);
        var second = await factory.CreateEpisodeOfCareAsync(CreateRequest(patientId), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess, second.Error?.Message);
        Assert.Equal(first.Value.PatientReference, second.Value.PatientReference);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsNotFoundError_WhenThePatientIsNotInTheDatabase()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IEpisodeOfCareFactory>();

        // Act
        var result = await factory.CreateEpisodeOfCareAsync(CreateRequest(patientId: -1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Core.Results.ErrorType.NotFound, result.Error?.Type);
    }

    private static async Task<long> GetSeededPatientIdAsync(IServiceProvider services, string cpr)
    {
        var patientService = services.GetRequiredService<IPatientService>();
        var patients = await patientService.GetAllPatientsAsync(TestContext.Current.CancellationToken);

        return patients.Single(patient => patient.Cpr == cpr).Id;
    }

    private static CreateEpisodeOfCareModel CreateRequest(long patientId) => new()
    {
        PatientId = patientId,
        Status = EpisodeOfCareStatusModel.Active,
        Start = _start
    };
}
