namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// One page of a patient search on the FHIR server.
/// </summary>
public sealed record PatientSearchResultModel
{
    /// <summary>The patients on this page.</summary>
    public required IReadOnlyList<FhirPatientModel> Patients { get; init; }

    /// <summary>How many patients match across every page, when the server counts them.</summary>
    public int? Total { get; init; }
}
