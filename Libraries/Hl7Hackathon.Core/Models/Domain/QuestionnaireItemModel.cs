namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// One group or question in a questionnaire.
/// </summary>
public sealed record QuestionnaireItemModel
{
    /// <summary>The id that ties an answer back to this question.</summary>
    public required string LinkId { get; init; }

    /// <summary>The question or group heading as the questionnaire words it.</summary>
    public string? Text { get; init; }

    /// <summary>Whether this is a heading over nested questions rather than a question itself.</summary>
    public required bool IsGroup { get; init; }

    /// <summary>The answers this question allows, empty when it is a group or takes a free answer.</summary>
    public required IReadOnlyList<AnswerOptionModel> Options { get; init; }

    /// <summary>The questions nested under this group.</summary>
    public required IReadOnlyList<QuestionnaireItemModel> Items { get; init; }
}
