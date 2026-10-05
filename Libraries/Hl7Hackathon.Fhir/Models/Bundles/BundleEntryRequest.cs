using Hl7Hackathon.Fhir.Codes;

namespace Hl7Hackathon.Fhir.Models.Bundles;

/// <summary>
/// Bundle.entry.request: the HTTP request a transaction or batch entry stands for.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/bundle-definitions.html#Bundle.entry.request">Bundle.entry.request (R4)</see>.</remarks>
public sealed record BundleEntryRequest
{
    /// <summary>GET | HEAD | POST | PUT | DELETE | PATCH</summary>
    public required HttpVerb Method { get; init; }

    /// <summary>URL for HTTP equivalent of this entry.</summary>
    /// <remarks>Relative to the server base, eg. "Patient" for a create.</remarks>
    public required string Url { get; init; }

    /// <summary>For conditional creates.</summary>
    /// <remarks>Only create when this search matches nothing, eg. "identifier=system|value".</remarks>
    public string? IfNoneExist { get; init; }
}
