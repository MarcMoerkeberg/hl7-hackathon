using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A concept, as codes in one or more code systems and/or as plain text.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#CodeableConcept">CodeableConcept (R4)</see>.</remarks>
public sealed record CodeableConcept
{
    /// <summary>Code defined by a terminology system.</summary>
    [MinLength(1)]
    public IReadOnlyList<Coding>? Coding { get; init; }

    /// <summary>Plain text representation of the concept.</summary>
    public string? Text { get; init; }
}
