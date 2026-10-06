using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.TestHelpers;
using FhirQuestionnaire = Hl7Hackathon.Fhir.Models.Questionnaires.Questionnaire;

namespace Hl7Hackathon.UnitTests.Mappers;

[Trait("Category", "Unit")]
public class QuestionnaireMapperTests
{
    private const string PatientReference = "Patient/7";
    private const string EpisodeOfCareReference = "EpisodeOfCare/42";
    private const string FriendsCode = "60e95414-b97d-4197-af85-d5efc5628e86";
    private const string LovedCode = "a5341a86-677d-4d53-84a1-498dc1a4ed42";

    private static readonly DateTimeOffset _authored = new(2026, 10, 8, 9, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void ToModel_KeepsTheGroupsAndTheirQuestions()
    {
        // Act
        var model = QuestionnaireMapper.ToModel(TestFhirResources.TelemedicineQuestionnaire() with { Id = "mental-health" });

        // Assert
        Assert.Equal("mental-health", model.Id);
        Assert.Equal(TestFhirResources.QuestionnaireUrl, model.Url);
        var group = Assert.Single(model.Items);
        Assert.True(group.IsGroup);
        Assert.Empty(group.Options);
        Assert.Equal(2, group.Items.Count);

        var question = group.Items[0];
        Assert.False(question.IsGroup);
        Assert.Equal("Venskaber", question.Text);
        Assert.Equal(3, question.Options.Count);
        Assert.Equal(FriendsCode, question.Options[0].Code);
        Assert.Equal(TestFhirResources.MentalHealthCodeSystem, question.Options[0].System);
    }

    [Fact]
    public void FindAnswerOption_FindsAnOptionOnANestedQuestion()
    {
        // Act
        var coding = QuestionnaireMapper.FindAnswerOption(TestFhirResources.TelemedicineQuestionnaire(), "1.1", FriendsCode);

        // Assert
        Assert.Equal(TestFhirResources.MentalHealthCodeSystem, coding?.System);
        Assert.Equal("Jeg føler mig tæt på mine venner", coding?.Display);
    }

    [Theory]
    [InlineData("9.9", FriendsCode)]
    [InlineData("1.1", "not-an-option")]
    [InlineData("1", FriendsCode)]
    public void FindAnswerOption_ReturnsNull_WhenTheQuestionnaireDoesNotAllowIt(string linkId, string code)
    {
        // Act
        var coding = QuestionnaireMapper.FindAnswerOption(TestFhirResources.TelemedicineQuestionnaire(), linkId, code);

        // Assert
        Assert.Null(coding);
    }

    [Fact]
    public void ToFhir_BuildsAResponseThatMeetsTheContract()
    {
        // Arrange
        var questionnaire = TestFhirResources.TelemedicineQuestionnaire();

        // Act
        var response = QuestionnaireMapper.ToFhir(
            CreateRequest(),
            questionnaire,
            Answers(questionnaire, ("1.1", FriendsCode), ("1.2", LovedCode)),
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        Assert.Empty(FhirModelValidator.Validate(response));
        Assert.True(response.ClaimsProfile(Telemedicine.Profiles.TelemedicineQuestionnaireResponse));
        Assert.Equal(TestFhirResources.QuestionnaireUrl, response.Questionnaire);
        Assert.Equal(PatientReference, response.Subject?.Reference);
        Assert.Equal(PatientReference, response.Source?.Reference);
        Assert.Equal("2026-10-08T09:00:00+02:00", response.Authored);
        Assert.Equal(EpisodeOfCareReference, Assert.Single(response.GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl)).ValueReference?.Reference);
    }

    [Fact]
    public void ToFhir_NestsTheAnswersUnderTheirGroup()
    {
        // Arrange
        var questionnaire = TestFhirResources.TelemedicineQuestionnaire();

        // Act
        var response = QuestionnaireMapper.ToFhir(
            CreateRequest(),
            questionnaire,
            Answers(questionnaire, ("1.1", FriendsCode)),
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        var group = Assert.Single(response.Item!);
        Assert.Equal("1", group.LinkId);
        Assert.Null(group.Answer);

        var question = Assert.Single(group.Item!);
        Assert.Equal("1.1", question.LinkId);
        Assert.Equal("Venskaber", question.Text);

        // The answer carries the questionnaire's own system and display, not something this app made up.
        var answer = Assert.Single(question.Answer!);
        Assert.Equal(FriendsCode, answer.ValueCoding?.Code);
        Assert.Equal(TestFhirResources.MentalHealthCodeSystem, answer.ValueCoding?.System);
        Assert.Equal("Jeg føler mig tæt på mine venner", answer.ValueCoding?.Display);
    }

    [Fact]
    public void ToFhir_LeavesOutGroupsWithNothingAnsweredUnderThem()
    {
        // Arrange
        var questionnaire = TestFhirResources.TelemedicineQuestionnaire();

        // Act
        var response = QuestionnaireMapper.ToFhir(
            CreateRequest(),
            questionnaire,
            new Dictionary<string, Coding>(StringComparer.Ordinal),
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        // FHIR forbids empty arrays, so an untouched questionnaire sends no item at all.
        Assert.Null(response.Item);
    }

    [Fact]
    public void ToModel_CountsTheAnswersAcrossTheGroups()
    {
        // Arrange
        var questionnaire = TestFhirResources.TelemedicineQuestionnaire();
        var response = QuestionnaireMapper.ToFhir(
            CreateRequest(),
            questionnaire,
            Answers(questionnaire, ("1.1", FriendsCode), ("1.2", LovedCode)),
            PatientReference,
            EpisodeOfCareReference) with { Id = "77" };

        // Act
        var model = QuestionnaireMapper.ToModel(response);

        // Assert
        Assert.Equal("77", model.Id);
        Assert.Equal("completed", model.Status);
        Assert.Equal(TestFhirResources.QuestionnaireUrl, model.Questionnaire);
        Assert.Equal(PatientReference, model.PatientReference);
        Assert.Equal(EpisodeOfCareReference, model.EpisodeOfCareReference);
        Assert.Equal(2, model.AnswerCount);
    }

    private static Dictionary<string, Coding> Answers(
        FhirQuestionnaire questionnaire,
        params (string LinkId, string Code)[] answers)
    {
        return answers.ToDictionary(
            answer => answer.LinkId,
            answer => QuestionnaireMapper.FindAnswerOption(questionnaire, answer.LinkId, answer.Code)!,
            StringComparer.Ordinal);
    }

    private static CreateQuestionnaireResponseModel CreateRequest() => new()
    {
        EpisodeOfCareId = "42",
        QuestionnaireUrl = TestFhirResources.QuestionnaireUrl,
        Authored = _authored,
        Answers = [new QuestionnaireAnswerModel { LinkId = "1.1", Code = FriendsCode }]
    };
}
