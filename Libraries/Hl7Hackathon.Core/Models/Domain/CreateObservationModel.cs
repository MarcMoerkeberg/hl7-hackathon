namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A measurement to report on an episode of care.
/// </summary>
/// <remarks>
/// The patient is not named here: both profiles tie the observation to an episode of care, and the episode on the
/// server already says which patient it is for.
/// </remarks>
public sealed record CreateObservationModel
{
    /// <summary>The id of the episode of care on the FHIR server.</summary>
    public required string EpisodeOfCareId { get; init; }

    public required ObservationKindModel Kind { get; init; }

    /// <summary>The start of the period the measurement covers.</summary>
    public required DateTimeOffset Start { get; init; }

    /// <summary>The end of the period the measurement covers.</summary>
    public required DateTimeOffset End { get; init; }

    /// <summary>Hours of sleep, or minutes of activity. Required for sleep.</summary>
    public decimal? Value { get; init; }

    /// <summary>The SNOMED CT code of the activity. Required for physical activity.</summary>
    public string? ActivityCode { get; init; }
}
