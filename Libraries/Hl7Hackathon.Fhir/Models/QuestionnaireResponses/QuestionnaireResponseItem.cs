using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Models.QuestionnaireResponses;

/// <summary>
/// QuestionnaireResponse.item: a group or question, mirroring the item tree of the Questionnaire by <see cref="LinkId"/>.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaireResponse.html">TelemedicineQuestionnaireResponse</see>
/// and <see href="https://hl7.org/fhir/R4/questionnaireresponse-definitions.html#QuestionnaireResponse.item">QuestionnaireResponse.item (R4)</see>.
/// </remarks>
public sealed record QuestionnaireResponseItem
{
    /// <summary>Pointer to specific item from Questionnaire.</summary>
    public required string LinkId { get; init; }

    /// <summary>Name for group or question text.</summary>
    public string? Text { get; init; }

    /// <summary>The response(s) to the question.</summary>
    [MinLength(1)]
    public IReadOnlyList<QuestionnaireResponseAnswer>? Answer { get; init; }

    /// <summary>Nested questionnaire response items.</summary>
    [MinLength(1)]
    public IReadOnlyList<QuestionnaireResponseItem>? Item { get; init; }
}
