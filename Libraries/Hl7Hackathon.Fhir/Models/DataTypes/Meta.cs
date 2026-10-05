using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// Metadata about a resource: its version, when it last changed and the profiles it claims to conform to.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/resource.html#Meta">Meta (R4)</see>.</remarks>
public sealed record Meta
{
    /// <summary>Version specific identifier.</summary>
    /// <remarks>Assigned by the server.</remarks>
    public string? VersionId { get; init; }

    /// <summary>When the resource version last changed.</summary>
    /// <remarks>Assigned by the server; a FHIR <c>instant</c>.</remarks>
    public string? LastUpdated { get; init; }

    /// <summary>Profiles this resource claims to conform to.</summary>
    /// <remarks>The hackathon server validates a resource against the profiles listed here, so always set the IG profile.</remarks>
    [MinLength(1)]
    public IReadOnlyList<string>? Profile { get; init; }
}
