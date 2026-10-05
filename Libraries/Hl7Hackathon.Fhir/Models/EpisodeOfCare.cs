using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Models;

/// <summary>
/// A telemedicine course: the period in which a patient is followed, which observations and answers are linked to.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.dk/fhir/hackathons/StructureDefinition-TelemedicineEpisodeOfCare.html">TelemedicineEpisodeOfCare</see>,
/// based on <see href="https://hl7.org/fhir/R4/episodeofcare.html">EpisodeOfCare (R4)</see>.
/// </remarks>
public sealed record EpisodeOfCare : DomainResource, IValidatableObject
{
    /// <summary>planned | waitlist | active | onhold | finished | cancelled | entered-in-error</summary>
    public required EpisodeOfCareStatus Status { get; init; }

    /// <summary>The patient who is the focus of this episode of care.</summary>
    /// <remarks>Must reference a TelemedicinePatient.</remarks>
    public required ResourceReference Patient { get; init; }

    /// <summary>Interval during responsibility is assumed.</summary>
    [Required]
    public Period? Period { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ClaimsProfile(Telemedicine.Profiles.TelemedicineEpisodeOfCare))
        {
            yield return new ValidationResult($"meta.profile must include {Telemedicine.Profiles.TelemedicineEpisodeOfCare}.", [nameof(Meta)]);
        }
    }
}
