using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Models.Questionnaires;

/// <summary>
/// Questionnaire.item.answerOption: one permitted answer to a question.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaire.html">TelemedicineQuestionnaire</see>
/// and <see href="https://hl7.org/fhir/R4/questionnaire-definitions.html#Questionnaire.item.answerOption">Questionnaire.item.answerOption (R4)</see>.
/// </remarks>
public sealed record QuestionnaireAnswerOption
{
    /// <summary>Answer value.</summary>
    /// <remarks>value[x]: the IG questionnaire only uses coded options, so only valueCoding is modelled.</remarks>
    public Coding? ValueCoding { get; init; }
}
