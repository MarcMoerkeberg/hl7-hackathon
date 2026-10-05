using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;

namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A name of a person, split into its parts.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#HumanName">HumanName (R4)</see>.</remarks>
public sealed record HumanName
{
    /// <summary>usual | official | temp | nickname | anonymous | old | maiden</summary>
    /// <remarks>DK Core expects the name from the CPR register as "official".</remarks>
    public NameUse? Use { get; init; }

    /// <summary>Family name (often called 'Surname').</summary>
    public string? Family { get; init; }

    /// <summary>Given names (not always 'first'). Includes middle names.</summary>
    [MinLength(1)]
    public IReadOnlyList<string>? Given { get; init; }
}
