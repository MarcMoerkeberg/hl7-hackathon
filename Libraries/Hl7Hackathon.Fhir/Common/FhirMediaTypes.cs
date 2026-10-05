namespace Hl7Hackathon.Fhir.Common;

public static class FhirMediaTypes
{
    /// <summary>The FHIR JSON media type, used for both the Accept and the Content-Type header.</summary>
    /// <remarks>See https://hl7.org/fhir/R4/http.html#mime-type.</remarks>
    public const string Json = "application/fhir+json";
}
