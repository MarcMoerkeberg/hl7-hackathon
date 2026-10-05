namespace Hl7Hackathon.Fhir.Models.Bundles;

/// <summary>
/// Bundle.link: a link related to the bundle, eg. the "self", "next" and "previous" pages of a search.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/bundle-definitions.html#Bundle.link">Bundle.link (R4)</see>.</remarks>
public sealed record BundleLink
{
    /// <summary>The relation, from the <see href="https://www.iana.org/assignments/link-relations/link-relations.xhtml">IANA link relations</see>.</summary>
    public required string Relation { get; init; }

    /// <summary>Reference details for the link.</summary>
    public required string Url { get; init; }
}
