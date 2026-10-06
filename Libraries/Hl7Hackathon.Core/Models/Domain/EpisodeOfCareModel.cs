namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A telemedicine course as it is stored on the FHIR server.
/// </summary>
public sealed record EpisodeOfCareModel
{
    /// <summary>The id the FHIR server gave the episode of care.</summary>
    public required string Id { get; init; }

    /// <summary>The status code as the server returned it, which may be one we never send.</summary>
    public required string Status { get; init; }

    /// <summary>The reference to the patient, eg. "Patient/123".</summary>
    public string? PatientReference { get; init; }

    /// <summary>The start of the course. A string because a FHIR dateTime may be partial ("2026" or "2026-10").</summary>
    public string? Start { get; init; }

    /// <summary>The end of the course, when the server has one.</summary>
    public string? End { get; init; }
}
