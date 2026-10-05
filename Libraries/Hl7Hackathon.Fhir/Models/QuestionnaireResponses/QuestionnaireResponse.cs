using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.Questionnaires;
using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.Fhir.Models.QuestionnaireResponses;

/// <summary>
/// The patient's answers to the mental health questionnaire.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineQuestionnaireResponse.html">TelemedicineQuestionnaireResponse</see>,
/// based on <see href="https://hl7.org/fhir/R4/questionnaireresponse.html">QuestionnaireResponse (R4)</see>. The
/// profile requires the workflow-episodeOfCare extension.
/// </remarks>
public sealed record QuestionnaireResponse : DomainResource, IValidatableObject
{
    /// <summary>Form being answered.</summary>
    /// <remarks>The canonical URL of the Questionnaire, not a "Questionnaire/[id]" reference.</remarks>
    [Required]
    public string? Questionnaire { get; init; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped</summary>
    public required QuestionnaireResponseStatus Status { get; init; }

    /// <summary>The subject of the questions.</summary>
    [Required]
    public ResourceReference? Subject { get; init; }

    /// <summary>Date the answers were gathered.</summary>
    [Required]
    [FhirDateTime]
    public string? Authored { get; init; }

    /// <summary>The person who answered the questions.</summary>
    /// <remarks>Must reference a TelemedicinePatient, Practitioner or RelatedPerson.</remarks>
    [Required]
    public ResourceReference? Source { get; init; }

    /// <summary>Groups and questions.</summary>
    [MinLength(1)]
    public IReadOnlyList<QuestionnaireResponseItem>? Item { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ClaimsProfile(Telemedicine.Profiles.TelemedicineQuestionnaireResponse))
        {
            yield return new ValidationResult(
                $"meta.profile must include {Telemedicine.Profiles.TelemedicineQuestionnaireResponse}.",
                [nameof(Meta)]);
        }

        if (GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl) is not [{ ValueReference.Reference: not null }])
        {
            yield return new ValidationResult(
                "Must have exactly one workflow-episodeOfCare extension that references the EpisodeOfCare.",
                [nameof(Extension)]);
        }
    }
}
