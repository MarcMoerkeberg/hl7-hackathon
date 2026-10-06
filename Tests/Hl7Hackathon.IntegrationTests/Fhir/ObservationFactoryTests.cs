using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Fhir;

/// <summary>
/// Reports sleep and physical activity against a throwaway HAPI server, through the whole flow: a seeded patient,
/// an episode of care, then the observations on it.
/// </summary>
[Collection(FhirServerTestsCollection.Name)]
[Trait("Category", "FhirServer")]
public class ObservationFactoryTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));

    private readonly FhirServerFixture _fixture;

    public ObservationFactoryTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateObservationAsync_ReportsSleepOnTheEpisodeOfCare()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IObservationFactory>();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var episodeOfCare = await GivenAnEpisodeOfCareAsync(scope.ServiceProvider, "1506999990");

        // Act
        var result = await factory.CreateObservationAsync(
            CreateRequest(episodeOfCare.Id, ObservationKindModel.Sleep) with { Value = 7.5m },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(ObservationKindModel.Sleep, result.Value.Kind);
        Assert.Equal(7.5m, result.Value.Value);
        Assert.Equal("Hours", result.Value.Unit);
        Assert.Equal(episodeOfCare.PatientReference, result.Value.PatientReference);
        Assert.Equal($"EpisodeOfCare/{episodeOfCare.Id}", result.Value.EpisodeOfCareReference);

        var read = await fhirService.GetResourceByIdAsync<Observation>(result.Value.Id, TestContext.Current.CancellationToken);
        Assert.True(read.IsSuccess, read.Error?.Message);
        Assert.Equal("248263006", read.Value.Code.Coding?[0].Code);
    }

    [Fact]
    public async Task CreateObservationAsync_ReportsPhysicalActivityWithTheChosenCode()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IObservationFactory>();
        var episodeOfCare = await GivenAnEpisodeOfCareAsync(scope.ServiceProvider, "0311069994");
        var walking = factory.GetActivityCodes().Single(activity => activity.Display == "Walking");

        // Act
        var result = await factory.CreateObservationAsync(
            CreateRequest(episodeOfCare.Id, ObservationKindModel.PhysicalActivity) with
            {
                Value = 45m,
                ActivityCode = walking.Code
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(ObservationKindModel.PhysicalActivity, result.Value.Kind);
        Assert.Equal("Walking", result.Value.Code);
        Assert.Equal("Minutes", result.Value.Unit);
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsNotFoundError_WhenTheEpisodeOfCareIsNotOnTheServer()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IObservationFactory>();

        // Act
        var result = await factory.CreateObservationAsync(
            CreateRequest("does-not-exist", ObservationKindModel.Sleep) with { Value = 7m },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_FindsTheEpisodeThroughTheChainedCprSearch()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var episodeOfCareFactory = scope.ServiceProvider.GetRequiredService<IEpisodeOfCareFactory>();
        var patientId = await GetSeededPatientIdAsync(scope.ServiceProvider, "1203049991");
        var created = await episodeOfCareFactory.CreateEpisodeOfCareAsync(
            new CreateEpisodeOfCareModel
            {
                PatientId = patientId,
                Status = EpisodeOfCareStatusModel.Active,
                Start = _start
            },
            TestContext.Current.CancellationToken);
        Assert.True(created.IsSuccess, created.Error?.Message);

        // Act
        var result = await episodeOfCareFactory.GetEpisodesOfCareForPatientAsync(patientId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains(result.Value, episodeOfCare => episodeOfCare.Id == created.Value.Id);
    }

    private static async Task<EpisodeOfCareModel> GivenAnEpisodeOfCareAsync(IServiceProvider services, string cpr)
    {
        var factory = services.GetRequiredService<IEpisodeOfCareFactory>();
        var patientId = await GetSeededPatientIdAsync(services, cpr);
        var created = await factory.CreateEpisodeOfCareAsync(
            new CreateEpisodeOfCareModel
            {
                PatientId = patientId,
                Status = EpisodeOfCareStatusModel.Active,
                Start = _start
            },
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value;
    }

    private static async Task<long> GetSeededPatientIdAsync(IServiceProvider services, string cpr)
    {
        var patientService = services.GetRequiredService<IPatientService>();
        var patients = await patientService.GetAllPatientsAsync(TestContext.Current.CancellationToken);

        return patients.Single(patient => patient.Cpr == cpr).Id;
    }

    private static CreateObservationModel CreateRequest(string episodeOfCareId, ObservationKindModel kind) => new()
    {
        EpisodeOfCareId = episodeOfCareId,
        Kind = kind,
        Start = _start,
        End = _start.AddDays(1)
    };
}
