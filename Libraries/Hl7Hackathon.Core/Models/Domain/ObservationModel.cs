namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A measurement as it is stored on the FHIR server.
/// </summary>
public sealed record ObservationModel
{
    /// <summary>The id the FHIR server gave the observation.</summary>
    public required string Id { get; init; }

    /// <summary>The status code as the server returned it.</summary>
    public required string Status { get; init; }

    /// <summary>The kind the observation's profile says it is, or <see langword="null"/> for a profile we do not know.</summary>
    public ObservationKindModel? Kind { get; init; }

    /// <summary>What was measured, eg. "Walking".</summary>
    public string? Code { get; init; }

    public string? PatientReference { get; init; }

    public string? EpisodeOfCareReference { get; init; }

    /// <summary>The start of the period. A string because a FHIR dateTime may be partial.</summary>
    public string? Start { get; init; }

    public string? End { get; init; }

    public decimal? Value { get; init; }

    /// <summary>"Hours" or "Minutes".</summary>
    public string? Unit { get; init; }
}
