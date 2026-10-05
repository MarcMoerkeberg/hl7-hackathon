namespace Hl7Hackathon.Fhir.Models.Bundles;

/// <summary>
/// Bundle.entry: one resource in a bundle, with what to do or what was done with it.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/bundle-definitions.html#Bundle.entry">Bundle.entry (R4)</see>.</remarks>
public sealed record BundleEntry
{
    /// <summary>URI for resource (Absolute URL server address or URI for UUID/OID).</summary>
    /// <remarks>
    /// In a transaction a new resource gets a "urn:uuid:..." placeholder that other entries can reference; the server
    /// replaces those references with the real ids.
    /// </remarks>
    public string? FullUrl { get; init; }

    /// <summary>A resource in the bundle.</summary>
    public Resource? Resource { get; init; }

    /// <summary>Additional execution information (transaction/batch/history).</summary>
    /// <remarks>What the server should do with this entry.</remarks>
    public BundleEntryRequest? Request { get; init; }

    /// <summary>Results of execution (transaction/batch/history).</summary>
    /// <remarks>What the server did with this entry.</remarks>
    public BundleEntryResponse? Response { get; init; }
}
