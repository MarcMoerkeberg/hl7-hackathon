namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A filled-in questionnaire to report on an episode of care.
/// </summary>
/// <remarks>
/// The patient is not named here: the episode of care on the server already says which patient it is for.
/// </remarks>
public sealed record CreateQuestionnaireResponseModel
{
    /// <summary>The id of the episode of care on the FHIR server.</summary>
    public required string EpisodeOfCareId { get; init; }

    /// <summary>The canonical URL of the questionnaire being answered.</summary>
    public required string QuestionnaireUrl { get; init; }

    /// <summary>When the answers were given.</summary>
    public required DateTimeOffset Authored { get; init; }

    public required IReadOnlyList<QuestionnaireAnswerModel> Answers { get; init; }
}
