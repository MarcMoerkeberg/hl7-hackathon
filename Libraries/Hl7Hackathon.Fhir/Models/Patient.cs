using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.Fhir.Models;

/// <summary>
/// A patient receiving telemedicine care.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicinePatient.html">TelemedicinePatient</see>,
/// based on <see href="https://hl7.dk/fhir/core/StructureDefinition-dk-core-patient.html">DkCorePatient</see> and
/// <see href="https://hl7.org/fhir/R4/patient.html">Patient (R4)</see>.
/// </remarks>
public sealed record Patient : DomainResource, IValidatableObject
{
    /// <summary>An identifier for this patient.</summary>
    /// <remarks>DK Core requires at least one, normally the CPR number.</remarks>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<Identifier>? Identifier { get; init; }

    /// <summary>A name associated with the patient.</summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<HumanName>? Name { get; init; }

    /// <summary>male | female | other | unknown</summary>
    public AdministrativeGender? Gender { get; init; }

    /// <summary>The date of birth for the individual.</summary>
    [FhirDate]
    public string? BirthDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ClaimsProfile(Telemedicine.Profiles.TelemedicinePatient))
        {
            yield return new ValidationResult($"meta.profile must include {Telemedicine.Profiles.TelemedicinePatient}.", [nameof(Meta)]);
        }
    }
}
