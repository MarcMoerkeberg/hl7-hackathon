namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The HTTP verb of a transaction or batch entry (HTTPVerb).
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-http-verb.html">http-verb (R4)</see>.</remarks>
public sealed record HttpVerb : FhirCode<HttpVerb>
{
    /// <summary>HTTP GET Command.</summary>
    public static readonly HttpVerb Get = new("GET");

    /// <summary>HTTP HEAD Command.</summary>
    public static readonly HttpVerb Head = new("HEAD");

    /// <summary>HTTP POST Command.</summary>
    public static readonly HttpVerb Post = new("POST");

    /// <summary>HTTP PUT Command.</summary>
    public static readonly HttpVerb Put = new("PUT");

    /// <summary>HTTP DELETE Command.</summary>
    public static readonly HttpVerb Delete = new("DELETE");

    /// <summary>HTTP PATCH Command.</summary>
    public static readonly HttpVerb Patch = new("PATCH");

    private HttpVerb(string code)
        : base(code)
    {
    }
}
