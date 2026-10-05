namespace Hl7Hackathon.Fhir.Models.CapabilityStatements;

/// <summary>
/// CapabilityStatement.software: the server software.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/capabilitystatement-definitions.html#CapabilityStatement.software">CapabilityStatement.software (R4)</see>.</remarks>
public sealed record CapabilityStatementSoftware
{
    /// <summary>A name the software is known by.</summary>
    public required string Name { get; init; }

    /// <summary>Version covered by this statement.</summary>
    public string? Version { get; init; }
}
