namespace Hl7Hackathon.Fhir.Models.CapabilityStatements;

/// <summary>
/// CapabilityStatement.rest: the RESTful interface the server offers.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/capabilitystatement-definitions.html#CapabilityStatement.rest">CapabilityStatement.rest (R4)</see>.</remarks>
public sealed record CapabilityStatementRest
{
    /// <summary>client | server</summary>
    public required string Mode { get; init; }

    /// <summary>Resource served on the REST interface.</summary>
    public IReadOnlyList<CapabilityStatementResource>? Resource { get; init; }
}
