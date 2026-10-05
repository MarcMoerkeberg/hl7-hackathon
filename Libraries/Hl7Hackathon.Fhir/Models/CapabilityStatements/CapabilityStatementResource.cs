namespace Hl7Hackathon.Fhir.Models.CapabilityStatements;

/// <summary>
/// CapabilityStatement.rest.resource: one resource type the server supports.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/capabilitystatement-definitions.html#CapabilityStatement.rest.resource">CapabilityStatement.rest.resource (R4)</see>.</remarks>
public sealed record CapabilityStatementResource
{
    /// <summary>A resource type that is supported.</summary>
    public required string Type { get; init; }

    /// <summary>Profiles for use cases supported.</summary>
    /// <remarks>The hackathon server should list the IG profiles here.</remarks>
    public IReadOnlyList<string>? SupportedProfile { get; init; }

    /// <summary>Search parameters supported by implementation.</summary>
    public IReadOnlyList<CapabilityStatementSearchParam>? SearchParam { get; init; }
}
