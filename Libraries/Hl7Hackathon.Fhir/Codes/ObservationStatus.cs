namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The status of an observation.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-observation-status.html">observation-status (R4)</see>.</remarks>
public sealed record ObservationStatus : FhirCode<ObservationStatus>
{
    /// <summary>The existence of the observation is registered, but there is no result yet available.</summary>
    public static readonly ObservationStatus Registered = new("registered");

    /// <summary>This is an initial or interim observation: data may be incomplete or unverified.</summary>
    public static readonly ObservationStatus Preliminary = new("preliminary");

    /// <summary>The observation is complete and there are no further actions needed.</summary>
    public static readonly ObservationStatus Final = new("final");

    /// <summary>Subsequent to being Final, the observation has been modified. This includes updates/new information and corrections.</summary>
    public static readonly ObservationStatus Amended = new("amended");

    /// <summary>Subsequent to being Final, the observation has been modified to correct an error in the test result.</summary>
    public static readonly ObservationStatus Corrected = new("corrected");

    /// <summary>The observation is unavailable because the measurement was not started or not completed.</summary>
    public static readonly ObservationStatus Cancelled = new("cancelled");

    /// <summary>The observation has been withdrawn following previous final release.</summary>
    public static readonly ObservationStatus EnteredInError = new("entered-in-error");

    /// <summary>The authoring/source system does not know which of the status values currently applies for this observation.</summary>
    public static readonly ObservationStatus Unknown = new("unknown");

    private ObservationStatus(string code)
        : base(code)
    {
    }
}
