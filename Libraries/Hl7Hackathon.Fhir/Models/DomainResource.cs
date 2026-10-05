using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Fhir.Models;

/// <summary>
/// Every resource except Bundle, Parameters and Binary; these are the resources that can carry extensions.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/domainresource.html">DomainResource (R4)</see>.</remarks>
public abstract record DomainResource : Resource
{
    /// <summary>Additional content defined by implementations.</summary>
    [JsonPropertyOrder(-1)]
    [MinLength(1)]
    public IReadOnlyList<Extension>? Extension { get; init; }

    /// <summary>
    /// Gets the extensions identified by the <paramref name="url"/>.
    /// </summary>
    /// <param name="url">The URL that identifies the extension.</param>
    /// <returns>The matching extensions, or an empty list when there are none.</returns>
    public IReadOnlyList<Extension> GetExtensions(string url)
    {
        return Extension?.Where(extension => extension.Url == url).ToList() ?? [];
    }
}
