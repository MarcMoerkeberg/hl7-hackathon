namespace Hl7Hackathon.Fhir.Models.OperationOutcomes;

/// <summary>
/// The explanation of an error or a validation result, returned by the server.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.org/fhir/R4/operationoutcome.html">OperationOutcome (R4)</see>. Read from the
/// server only, so the model has no validation beyond the base cardinalities.
/// </remarks>
public sealed record OperationOutcome : DomainResource
{
    /// <summary>A single issue associated with the action.</summary>
    public required IReadOnlyList<OperationOutcomeIssue> Issue { get; init; }
}
