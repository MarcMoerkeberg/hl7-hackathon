using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Fhir;

/// <summary>
/// Answers a questionnaire on a throwaway HAPI server. HAPI does not ship the IG's mental health questionnaire, so
/// each test publishes its own under a fresh canonical URL and then answers that.
/// </summary>
[Collection(FhirServerTestsCollection.Name)]
[Trait("Category", "FhirServer")]
public class QuestionnaireFactoryTests
{
    private const string FriendsCode = "60e95414-b97d-4197-af85-d5efc5628e86";

    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));

    private readonly FhirServerFixture _fixture;

    public QuestionnaireFactoryTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetQuestionnairesAsync_ReturnsAQuestionnaireWithItsQuestionsAndOptions()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireFactory>();
        var url = await GivenAQuestionnaireOnTheServerAsync(scope.ServiceProvider);

        // Act
        var result = await factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        var questionnaire = Assert.Single(result.Value, candidate => candidate.Url == url);
        var group = Assert.Single(questionnaire.Items);
        Assert.True(group.IsGroup);
        Assert.Equal(3, group.Items[0].Options.Count);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_AnswersTheQuestionnaireOnTheEpisodeOfCare()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireFactory>();
        var fhirService = scope.ServiceProvider.GetRequiredService<IFhirService>();
        var url = await GivenAQuestionnaireOnTheServerAsync(scope.ServiceProvider);
        var episodeOfCare = await GivenAnEpisodeOfCareAsync(scope.ServiceProvider, "2512489996");

        // Act
        var result = await factory.CreateQuestionnaireResponseAsync(
            new CreateQuestionnaireResponseModel
            {
                EpisodeOfCareId = episodeOfCare.Id,
                QuestionnaireUrl = url,
                // The IG posts the questionnaire on the last of the reported days.
                Authored = _start.AddDays(2),
                Answers = [new QuestionnaireAnswerModel { LinkId = "1.1", Code = FriendsCode }]
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("completed", result.Value.Status);
        Assert.Equal(url, result.Value.Questionnaire);
        Assert.Equal(1, result.Value.AnswerCount);
        Assert.Equal(episodeOfCare.PatientReference, result.Value.PatientReference);
        Assert.Equal($"EpisodeOfCare/{episodeOfCare.Id}", result.Value.EpisodeOfCareReference);

        // Task 7 of the IG resolves the questionnaire from the response's canonical URL.
        var read = await fhirService.GetResourceByIdAsync<QuestionnaireResponse>(result.Value.Id, TestContext.Current.CancellationToken);
        Assert.True(read.IsSuccess, read.Error?.Message);
        Assert.Equal(url, read.Value.Questionnaire);
        Assert.Equal("1.1", Assert.Single(Assert.Single(read.Value.Item!).Item!).LinkId);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsNotFoundError_WhenNoQuestionnaireHasThatUrl()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireFactory>();
        var episodeOfCare = await GivenAnEpisodeOfCareAsync(scope.ServiceProvider, "1506999990");

        // Act
        var result = await factory.CreateQuestionnaireResponseAsync(
            new CreateQuestionnaireResponseModel
            {
                EpisodeOfCareId = episodeOfCare.Id,
                QuestionnaireUrl = $"http://example.org/Questionnaire/{Guid.NewGuid()}",
                Authored = _start,
                Answers = [new QuestionnaireAnswerModel { LinkId = "1.1", Code = FriendsCode }]
            },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    // A fresh canonical URL per test, so tests sharing the container never answer each other's questionnaire.
    private static async Task<string> GivenAQuestionnaireOnTheServerAsync(IServiceProvider services)
    {
        var url = $"http://hl7.dk/fhir/hackathons/Questionnaire/MentalHealth-{Guid.NewGuid()}";
        var fhirService = services.GetRequiredService<IFhirService>();
        var created = await fhirService.CreateResourceAsync(
            TestFhirResources.TelemedicineQuestionnaire(url),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Message);

        return url;
    }

    private static async Task<EpisodeOfCareModel> GivenAnEpisodeOfCareAsync(IServiceProvider services, string cpr)
    {
        var patientService = services.GetRequiredService<IPatientService>();
        var patients = await patientService.GetAllPatientsAsync(TestContext.Current.CancellationToken);
        var factory = services.GetRequiredService<IEpisodeOfCareFactory>();

        var created = await factory.CreateEpisodeOfCareAsync(
            new CreateEpisodeOfCareModel
            {
                PatientId = patients.Single(patient => patient.Cpr == cpr).Id,
                Status = EpisodeOfCareStatusModel.Active,
                Start = _start
            },
            TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Message);

        return created.Value;
    }
}
