using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Models.OperationOutcomes;

/// <summary>
/// OperationOutcome.issue: one error, warning or piece of information.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/operationoutcome-definitions.html#OperationOutcome.issue">OperationOutcome.issue (R4)</see>.</remarks>
public sealed record OperationOutcomeIssue
{
    /// <summary>fatal | error | warning | information</summary>
    public required string Severity { get; init; }

    /// <summary>Error or warning code.</summary>
    /// <remarks>invalid | structure | required | value | not-found | processing | ...</remarks>
    public required string Code { get; init; }

    /// <summary>Additional details about the error.</summary>
    public CodeableConcept? Details { get; init; }

    /// <summary>Additional diagnostic information about the issue.</summary>
    public string? Diagnostics { get; init; }

    /// <summary>FHIRPath of element(s) related to issue.</summary>
    /// <remarks>Eg. "Observation.effective".</remarks>
    public IReadOnlyList<string>? Expression { get; init; }
}
