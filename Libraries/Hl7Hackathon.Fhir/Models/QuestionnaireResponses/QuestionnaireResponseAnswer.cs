using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Models.QuestionnaireResponses;

/// <summary>
/// QuestionnaireResponse.item.answer: one answer to a question.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaireResponse.html">TelemedicineQuestionnaireResponse</see>
/// and <see href="https://hl7.org/fhir/R4/questionnaireresponse-definitions.html#QuestionnaireResponse.item.answer">QuestionnaireResponse.item.answer (R4)</see>.
/// </remarks>
public sealed record QuestionnaireResponseAnswer
{
    /// <summary>Single-valued answer to the question.</summary>
    /// <remarks>value[x]: the IG questionnaire only has coded answers, so only valueCoding is modelled.</remarks>
    public Coding? ValueCoding { get; init; }
}
