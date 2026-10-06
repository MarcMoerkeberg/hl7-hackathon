namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// A request to open a telemedicine course for a patient in the local database.
/// </summary>
public sealed record CreateEpisodeOfCareModel
{
    /// <summary>The id of the patient in the local database.</summary>
    public required long PatientId { get; init; }

    public required EpisodeOfCareStatusModel Status { get; init; }

    /// <summary>When the course starts. The offset is sent along, so the server keeps the Danish local time.</summary>
    public required DateTimeOffset Start { get; init; }

    /// <summary>When the course ends, when that is already known.</summary>
    public DateTimeOffset? End { get; init; }
}
