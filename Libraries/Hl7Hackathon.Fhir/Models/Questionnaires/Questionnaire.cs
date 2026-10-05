namespace Hl7Hackathon.Fhir.Models.Questionnaires;

/// <summary>
/// A form of questions; the IG publishes the mental health questionnaire (Questionnaire/MentalHealthQuestionnaire).
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaire.html">TelemedicineQuestionnaire</see>,
/// based on <see href="https://hl7.org/fhir/R4/questionnaire.html">Questionnaire (R4)</see>. We only read
/// questionnaires from the server, so the model has no validation beyond the base cardinalities.
/// </remarks>
public sealed record Questionnaire : DomainResource
{
    /// <summary>Canonical identifier for this questionnaire, represented as a URI (globally unique).</summary>
    /// <remarks>The value that QuestionnaireResponse.questionnaire points to.</remarks>
    public string? Url { get; init; }

    /// <summary>Name for this questionnaire (human friendly).</summary>
    public string? Title { get; init; }

    /// <summary>draft | active | retired | unknown</summary>
    public required string Status { get; init; }

    /// <summary>Questions and sections within the Questionnaire.</summary>
    public IReadOnlyList<QuestionnaireItem>? Item { get; init; }
}
