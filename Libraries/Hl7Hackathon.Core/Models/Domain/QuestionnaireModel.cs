namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A questionnaire published on the FHIR server, with the questions it asks.
/// </summary>
public sealed record QuestionnaireModel
{
    /// <summary>The id the questionnaire has on the FHIR server.</summary>
    public required string Id { get; init; }

    /// <summary>
    /// The canonical URL that an answer points to.
    /// </summary>
    /// <remarks>
    /// A response names the questionnaire by this URL, not by id, so a questionnaire without one cannot be answered.
    /// </remarks>
    public string? Url { get; init; }

    public string? Title { get; init; }

    public required IReadOnlyList<QuestionnaireItemModel> Items { get; init; }
}
