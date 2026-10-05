namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The status of an episode of care.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-episode-of-care-status.html">episode-of-care-status (R4)</see>.</remarks>
public sealed record EpisodeOfCareStatus : FhirCode<EpisodeOfCareStatus>
{
    /// <summary>This episode of care is planned to start at the date specified in the period.start.</summary>
    public static readonly EpisodeOfCareStatus Planned = new("planned");

    /// <summary>This episode has been placed on a waitlist, pending the episode being made active (or cancelled).</summary>
    public static readonly EpisodeOfCareStatus Waitlist = new("waitlist");

    /// <summary>This episode of care is current.</summary>
    public static readonly EpisodeOfCareStatus Active = new("active");

    /// <summary>This episode of care is on hold; the organization has limited responsibility for the patient.</summary>
    public static readonly EpisodeOfCareStatus OnHold = new("onhold");

    /// <summary>This episode of care is finished and the organization is not expecting to be providing further care to the patient.</summary>
    public static readonly EpisodeOfCareStatus Finished = new("finished");

    /// <summary>The episode of care was cancelled, or withdrawn from service.</summary>
    public static readonly EpisodeOfCareStatus Cancelled = new("cancelled");

    /// <summary>This instance should not have been part of this patient's medical record.</summary>
    public static readonly EpisodeOfCareStatus EnteredInError = new("entered-in-error");

    private EpisodeOfCareStatus(string code)
        : base(code)
    {
    }
}
