namespace Hl7Hackathon.Fhir.Models.CapabilityStatements;

/// <summary>
/// CapabilityStatement.rest.resource.searchParam: a search parameter the server accepts for the resource type.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/capabilitystatement-definitions.html#CapabilityStatement.rest.resource.searchParam">CapabilityStatement.rest.resource.searchParam (R4)</see>.</remarks>
public sealed record CapabilityStatementSearchParam
{
    /// <summary>Name of search parameter.</summary>
    public required string Name { get; init; }

    /// <summary>Source of definition for parameter.</summary>
    /// <remarks>The canonical URL of the SearchParameter that defines it.</remarks>
    public string? Definition { get; init; }

    /// <summary>number | date | string | token | reference | composite | quantity | uri | special</summary>
    public required string Type { get; init; }
}
