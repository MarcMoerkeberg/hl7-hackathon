using System.Text.Json.Serialization;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.CapabilityStatements;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.OperationOutcomes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Models.Questionnaires;

namespace Hl7Hackathon.Fhir.Models;

/// <summary>
/// The base of every resource. The JSON property <c>resourceType</c> says which resource an object is; a type that is
/// not listed here is read as a plain Resource.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/resource.html">Resource (R4)</see>.</remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "resourceType", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(Bundle), nameof(Bundle))]
[JsonDerivedType(typeof(CapabilityStatement), nameof(CapabilityStatement))]
[JsonDerivedType(typeof(EpisodeOfCare), nameof(EpisodeOfCare))]
[JsonDerivedType(typeof(Observation), nameof(Observation))]
[JsonDerivedType(typeof(OperationOutcome), nameof(OperationOutcome))]
[JsonDerivedType(typeof(Patient), nameof(Patient))]
[JsonDerivedType(typeof(Questionnaire), nameof(Questionnaire))]
[JsonDerivedType(typeof(QuestionnaireResponse), nameof(QuestionnaireResponse))]
// Base elements first, as in the spec examples; System.Text.Json would otherwise write them after the derived ones.
public record Resource
{
    /// <summary>Logical id of this artifact.</summary>
    /// <remarks>Assigned by the server on create; leave it null when posting a new resource.</remarks>
    [JsonPropertyOrder(-3)]
    public string? Id { get; init; }

    /// <summary>Metadata about the resource.</summary>
    [JsonPropertyOrder(-2)]
    public Meta? Meta { get; init; }

    /// <summary>
    /// Whether <see cref="Meta"/> claims that the resource conforms to the profile with the <paramref name="profileCanonical"/>.
    /// </summary>
    /// <param name="profileCanonical">The canonical URL of the profile.</param>
    /// <returns><see langword="true"/> when <c>meta.profile</c> lists the profile.</returns>
    public bool ClaimsProfile(string profileCanonical)
    {
        return Meta?.Profile?.Contains(profileCanonical, StringComparer.Ordinal) == true;
    }
}
