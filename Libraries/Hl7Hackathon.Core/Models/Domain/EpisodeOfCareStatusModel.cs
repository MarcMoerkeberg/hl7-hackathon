namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// The statuses an episode of care can be created with.
/// </summary>
/// <remarks>
/// A subset of the FHIR <see href="https://hl7.org/fhir/R4/valueset-episode-of-care-status.html">episode-of-care-status</see>
/// value set: "entered-in-error" is a correction of an existing episode, not something to open a new one with.
/// </remarks>
public enum EpisodeOfCareStatusModel
{
    /// <summary>Planned to start at the start of the period.</summary>
    Planned,

    /// <summary>On a waitlist, pending being made active or cancelled.</summary>
    Waitlist,

    /// <summary>Current.</summary>
    Active,

    /// <summary>On hold; the organization has limited responsibility for the patient.</summary>
    OnHold,

    /// <summary>Finished; no further care is expected.</summary>
    Finished,

    /// <summary>Cancelled, or withdrawn from service.</summary>
    Cancelled
}
