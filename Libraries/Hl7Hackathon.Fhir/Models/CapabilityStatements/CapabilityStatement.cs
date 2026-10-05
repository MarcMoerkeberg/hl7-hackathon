namespace Hl7Hackathon.Fhir.Models.CapabilityStatements;

/// <summary>
/// What the server supports, from GET [base]/metadata.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.org/fhir/R4/capabilitystatement.html">CapabilityStatement (R4)</see>. Read
/// from the server only, so the model has no validation beyond the base cardinalities.
/// </remarks>
public sealed record CapabilityStatement : DomainResource
{
    /// <summary>draft | active | retired | unknown</summary>
    public required string Status { get; init; }

    /// <summary>FHIR Version the system supports.</summary>
    /// <remarks>"4.0.1" for R4.</remarks>
    public required string FhirVersion { get; init; }

    /// <summary>Software that is covered by this capability statement.</summary>
    public CapabilityStatementSoftware? Software { get; init; }

    /// <summary>If the endpoint is a RESTful one.</summary>
    public IReadOnlyList<CapabilityStatementRest>? Rest { get; init; }
}
