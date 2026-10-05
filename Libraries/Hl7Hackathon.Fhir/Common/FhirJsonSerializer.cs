using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hl7Hackathon.Fhir.Models;

namespace Hl7Hackathon.Fhir.Common;

/// <summary>
/// Serializer specific to JSON FHIR.
/// </summary>
/// <remarks>See <see href="https://hl7.org/fhir/R4/json.html">FHIR JSON Format</see> for more information.</remarks>
public static class FhirJsonSerializer
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowOutOfOrderMetadataProperties = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new FhirCodeJsonConverterFactory() }
    };

    /// <summary>
    /// Serializes a FHIR resource to JSON.
    /// </summary>
    /// <param name="resource">The FHIR resource to serialize.</param>
    /// <returns>A JSON string representation of the FHIR resource.</returns>
    public static string Serialize(Resource resource)
    {
        return JsonSerializer.Serialize(resource, _serializerOptions);
    }

    /// <summary>
    /// Deserializes a JSON string into a FHIR resource.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized FHIR resource.</returns>
    public static Resource Deserialize(string json)
    {
        return JsonSerializer.Deserialize<Resource>(json, _serializerOptions)
            ?? throw new JsonException("The JSON was null instead of a resource.");
    }

    /// <summary>
    /// Deserializes a JSON string into a FHIR resource of type <typeparamref name="TResource"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the FHIR resource.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized FHIR resource of type <typeparamref name="TResource"/>.</returns>
    public static TResource Deserialize<TResource>(string json)
        where TResource : Resource
    {
        return JsonSerializer.Deserialize<TResource>(json, _serializerOptions)
            ?? throw new JsonException("The JSON was null instead of a resource.");
    }
}
