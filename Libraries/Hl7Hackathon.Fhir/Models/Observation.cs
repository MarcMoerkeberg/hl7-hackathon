using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.Fhir.Models;

/// <summary>
/// A measurement made by the patient at home: the hours slept or the minutes of physical activity in a period.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-SleepObservation.html">SleepObservation</see>
/// or <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-PhysicalActivityObservation.html">PhysicalActivityObservation</see>,
/// both based on <see href="https://hl7.org/fhir/R4/observation.html">Observation (R4)</see>. Both profiles require
/// the workflow-episodeOfCare extension and allow no <c>component</c>.
/// </remarks>
public sealed record Observation : DomainResource, IValidatableObject
{
    /// <summary>registered | preliminary | final | amended +</summary>
    public required ObservationStatus Status { get; init; }

    /// <summary>Type of observation (code / type).</summary>
    /// <remarks>Fixed for sleep, chosen from a value set for physical activity; see <see cref="ObservationKind"/>.</remarks>
    public required CodeableConcept Code { get; init; }

    /// <summary>Who and/or what the observation is about.</summary>
    /// <remarks>Must reference a TelemedicinePatient.</remarks>
    [Required]
    public ResourceReference? Subject { get; init; }

    /// <summary>Who is responsible for the observation.</summary>
    /// <remarks>Exactly one, the same TelemedicinePatient as <see cref="Subject"/>.</remarks>
    [Required]
    [Length(1, 1)]
    public IReadOnlyList<ResourceReference>? Performer { get; init; }

    /// <summary>Clinically relevant time for observation.</summary>
    /// <remarks>effective[x]: at most one variant is set, and the IG profiles only allow <see cref="EffectivePeriod"/>.</remarks>
    [FhirDateTime]
    public string? EffectiveDateTime { get; init; }

    /// <summary>Clinically relevant time-period for observation.</summary>
    [Required]
    public Period? EffectivePeriod { get; init; }

    /// <summary>Hours of sleep or minutes of activity.</summary>
    /// <remarks>value[x]: required for sleep, optional for physical activity.</remarks>
    public Quantity? ValueQuantity { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl) is not [{ ValueReference.Reference: not null }])
        {
            yield return new ValidationResult(
                "Must have exactly one workflow-episodeOfCare extension that references the EpisodeOfCare.",
                [nameof(Extension)]);
        }

        if (EffectiveDateTime is not null)
        {
            yield return new ValidationResult("effective[x] must be an effectivePeriod.", [nameof(EffectiveDateTime)]);
        }

        var kind = ObservationKind.FromClaimedProfile(this);
        if (kind is null)
        {
            var profiles = string.Join(" or ", ObservationKind.All.Select(observationKind => observationKind.Profile));
            yield return new ValidationResult($"meta.profile must include {profiles}.", [nameof(Meta)]);
            yield break;
        }

        if (kind.Code is { } fixedCode && Code.Coding?.Any(coding => coding.System == fixedCode.System && coding.Code == fixedCode.Code) != true)
        {
            yield return new ValidationResult($"code must include {fixedCode.System}#{fixedCode.Code} ({fixedCode.Display}).", [nameof(Code)]);
        }

        if (kind.IsValueRequired && ValueQuantity is null)
        {
            yield return new ValidationResult("valueQuantity is required for this kind of observation.", [nameof(ValueQuantity)]);
        }

        if (ValueQuantity?.Unit is { } unit && unit != kind.Unit)
        {
            yield return new ValidationResult($"valueQuantity.unit must be \"{kind.Unit}\".", [nameof(ValueQuantity)]);
        }
    }
}
