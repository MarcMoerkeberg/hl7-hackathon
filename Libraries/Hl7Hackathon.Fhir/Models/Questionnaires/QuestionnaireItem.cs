namespace Hl7Hackathon.Fhir.Models.Questionnaires;

/// <summary>
/// Questionnaire.item: a group or a question; groups nest further items.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaire.html">TelemedicineQuestionnaire</see>
/// and <see href="https://hl7.org/fhir/R4/questionnaire-definitions.html#Questionnaire.item">Questionnaire.item (R4)</see>.
/// </remarks>
public sealed record QuestionnaireItem
{
    /// <summary>Unique id for item in questionnaire.</summary>
    /// <remarks>
    /// Links an answer in a QuestionnaireResponse back to this question. 1..1 in R4, but not <c>required</c>, for
    /// the same reason as <see cref="Questionnaire.Status"/>; an item without one cannot be answered and is skipped.
    /// </remarks>
    public string? LinkId { get; init; }

    /// <summary>Primary text for the item.</summary>
    public string? Text { get; init; }

    /// <summary>group | display | boolean | decimal | integer | date | dateTime +</summary>
    /// <remarks>1..1 in R4, but not <c>required</c>, for the same reason as <see cref="Questionnaire.Status"/>.</remarks>
    public string? Type { get; init; }

    /// <summary>Permitted answer.</summary>
    public IReadOnlyList<QuestionnaireAnswerOption>? AnswerOption { get; init; }

    /// <summary>Valueset containing permitted answers.</summary>
    public string? AnswerValueSet { get; init; }

    /// <summary>Nested questionnaire items.</summary>
    public IReadOnlyList<QuestionnaireItem>? Item { get; init; }
}
