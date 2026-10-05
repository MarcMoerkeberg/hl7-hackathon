namespace Hl7Hackathon.Fhir.Models.Bundles;

/// <summary>
/// Bundle.entry.response: what the server did with a transaction or batch entry.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/bundle-definitions.html#Bundle.entry.response">Bundle.entry.response (R4)</see>.</remarks>
public sealed record BundleEntryResponse
{
    /// <summary>Status response code (text optional).</summary>
    /// <remarks>Eg. "201 Created".</remarks>
    public required string Status { get; init; }

    /// <summary>The location (if the operation returns a location).</summary>
    /// <remarks>Eg. "Patient/123/_history/1".</remarks>
    public string? Location { get; init; }
}
