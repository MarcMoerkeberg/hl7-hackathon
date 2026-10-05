namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// The FHIR <c>Reference</c> datatype: a reference from one resource to another.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.org/fhir/R4/references.html">Reference (R4)</see>. Renamed because C# does not
/// allow a property with the same name as its type.
/// </remarks>
public sealed record ResourceReference
{
    /// <summary>Literal reference, Relative, internal or absolute URL.</summary>
    /// <remarks>"[type]/[id]", an absolute URL, or inside a transaction the "urn:uuid:..." fullUrl of another entry.</remarks>
    public string? Reference { get; init; }

    /// <summary>Text alternative for the resource.</summary>
    public string? Display { get; init; }
}
