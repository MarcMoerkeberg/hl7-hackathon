using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using FhirQuestionnaire = Hl7Hackathon.Fhir.Models.Questionnaires.Questionnaire;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class QuestionnaireFactoryTests
{
    private const string PatientReference = "Patient/7";
    private const string FriendsCode = "60e95414-b97d-4197-af85-d5efc5628e86";

    private static readonly DateTimeOffset _authored = new(2026, 10, 8, 9, 0, 0, TimeSpan.FromHours(2));

    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
    private readonly QuestionnaireFactory _factory;

    public QuestionnaireFactoryTests()
    {
        _factory = new QuestionnaireFactory(_fhirService, NullLogger<QuestionnaireFactory>.Instance);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_ReturnsTheQuestionnairesOnTheServer()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var questionnaire = Assert.Single(result.Value);
        Assert.Equal(TestFhirResources.QuestionnaireUrl, questionnaire.Url);
        Assert.Equal("Mental health questionnaire", questionnaire.Title);
        Assert.Single(questionnaire.Items);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_SkipsEntriesThatAreNotQuestionnaires()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle
            {
                Type = BundleType.Searchset,
                Entry =
                [
                    new BundleEntry { Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = "mental-health" } },
                    new BundleEntry { Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = null } },
                    new BundleEntry { Resource = new Patient { Id = "7" } }
                ]
            });

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("mental-health", Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_SkipsQuestionnairesThatCannotBeAnswered()
    {
        // Arrange
        // A response points at a canonical URL, and other systems publish questionnaires without one.
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle
            {
                Type = BundleType.Searchset,
                Entry =
                [
                    new BundleEntry { Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = "answerable" } },
                    new BundleEntry { Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = "no-url", Url = null } }
                ]
            });

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("answerable", Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_ReadsAQuestionnaireThatIsMissingElementsR4Requires()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle
            {
                Type = BundleType.Searchset,
                Entry =
                [
                    new BundleEntry
                    {
                        Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = "someone-elses", Status = null }
                    }
                ]
            });

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("someone-elses", Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(Error.Unavailable("could not reach the FHIR server"));

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
    }

    [Fact]
    public async Task GetQuestionnairesAsync_ReturnsUnexpectedError_WhenTheFhirServiceThrows()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("the request timed out"));

        // Act
        var result = await _factory.GetQuestionnairesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsTheCreatedResponse()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        GivenTheEpisodeOfCareExists();
        GivenTheServerStoresTheResponse();

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("77", result.Value.Id);
        Assert.Equal("completed", result.Value.Status);
        Assert.Equal(1, result.Value.AnswerCount);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_SendsTheCodingThatTheQuestionnairePublishes()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        GivenTheEpisodeOfCareExists();
        GivenTheServerStoresTheResponse();

        // Act
        await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        var sent = CapturedResponse();
        Assert.Equal(TestFhirResources.QuestionnaireUrl, sent.Questionnaire);
        Assert.Equal(PatientReference, sent.Source?.Reference);

        var answer = Assert.Single(Assert.Single(Assert.Single(sent.Item!).Item!).Answer!);
        Assert.Equal(FriendsCode, answer.ValueCoding?.Code);
        Assert.Equal(TestFhirResources.MentalHealthCodeSystem, answer.ValueCoding?.System);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsValidationError_WhenNothingIsAnswered()
    {
        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(
            CreateRequest() with { Answers = [] },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsNotFoundError_WhenNoQuestionnaireHasThatUrl()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle { Type = BundleType.Searchset, Total = 0 });

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Theory]
    [InlineData("1.1", "not-an-option")]
    [InlineData("9.9", FriendsCode)]
    public async Task CreateQuestionnaireResponseAsync_ReturnsValidationError_WhenTheQuestionnaireDoesNotAllowTheAnswer(
        string linkId,
        string code)
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(
            CreateRequest() with { Answers = [new QuestionnaireAnswerModel { LinkId = linkId, Code = code }] },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_PassesOnTheErrorFromReadingTheEpisodeOfCare()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("EpisodeOfCare/42 was not found"));

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsValidationError_WhenTheEpisodeOfCareNamesNoPatient()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.TelemedicineEpisodeOfCare() with
            {
                Id = "42",
                Patient = new ResourceReference { Display = "a patient with no reference" }
            });

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        GivenTheEpisodeOfCareExists();
        _fhirService.CreateResourceAsync(Arg.Any<QuestionnaireResponse>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Error.Validation("QuestionnaireResponse does not meet its FHIR contract"));

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("QuestionnaireResponse does not meet its FHIR contract", result.Error?.Message);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsUnexpectedError_WhenTheServerReturnsNoId()
    {
        // Arrange
        GivenTheServerPublishesTheQuestionnaire();
        GivenTheEpisodeOfCareExists();
        _fhirService.CreateResourceAsync(Arg.Any<QuestionnaireResponse>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.TelemedicineQuestionnaireResponse(PatientReference));

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task CreateQuestionnaireResponseAsync_ReturnsUnexpectedError_WhenTheFhirServiceThrows()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("the request timed out"));

        // Act
        var result = await _factory.CreateQuestionnaireResponseAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private void GivenTheServerPublishesTheQuestionnaire()
    {
        _fhirService.SearchResourcesAsync<FhirQuestionnaire>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle
            {
                Type = BundleType.Searchset,
                Total = 1,
                Entry = [new BundleEntry { Resource = TestFhirResources.TelemedicineQuestionnaire() with { Id = "mental-health" } }]
            });
    }

    private void GivenTheEpisodeOfCareExists()
    {
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.TelemedicineEpisodeOfCare(PatientReference) with { Id = "42" });
    }

    private void GivenTheServerStoresTheResponse()
    {
        _fhirService.CreateResourceAsync(Arg.Any<QuestionnaireResponse>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => ((QuestionnaireResponse)call[0]) with { Id = "77" });
    }

    private QuestionnaireResponse CapturedResponse()
    {
        return (QuestionnaireResponse)_fhirService.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IFhirService.CreateResourceAsync))
            .GetArguments()[0]!;
    }

    private static CreateQuestionnaireResponseModel CreateRequest() => new()
    {
        EpisodeOfCareId = "42",
        QuestionnaireUrl = TestFhirResources.QuestionnaireUrl,
        Authored = _authored,
        Answers = [new QuestionnaireAnswerModel { LinkId = "1.1", Code = FriendsCode }]
    };
}
