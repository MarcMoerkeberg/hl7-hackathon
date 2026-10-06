namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A patient as they are stored on the FHIR server.
/// </summary>
/// <remarks>
/// Everything but the id is optional: the server holds patients from other systems, which need not carry a CPR
/// number or the elements this app sends.
/// </remarks>
public sealed record FhirPatientModel
{
    /// <summary>The id the patient has on the FHIR server.</summary>
    public required string Id { get; init; }

    public string? Cpr { get; init; }

    public string? GivenName { get; init; }

    public string? FamilyName { get; init; }

    public string? Gender { get; init; }

    /// <summary>The date of birth. A string because a FHIR date may be partial ("1948" or "1948-12").</summary>
    public string? BirthDate { get; init; }
}
