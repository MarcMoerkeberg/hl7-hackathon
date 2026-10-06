namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A filled-in questionnaire as it is stored on the FHIR server.
/// </summary>
public sealed record QuestionnaireResponseModel
{
    /// <summary>The id the FHIR server gave the response.</summary>
    public required string Id { get; init; }

    /// <summary>The status code as the server returned it.</summary>
    public required string Status { get; init; }

    /// <summary>The canonical URL of the questionnaire that was answered.</summary>
    public string? Questionnaire { get; init; }

    /// <summary>When the answers were given. A string because a FHIR dateTime may be partial.</summary>
    public string? Authored { get; init; }

    public string? PatientReference { get; init; }

    public string? EpisodeOfCareReference { get; init; }

    /// <summary>How many questions were answered.</summary>
    public required int AnswerCount { get; init; }
}
