namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A code from a terminology system, eg. SNOMED CT.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#Coding">Coding (R4)</see>.</remarks>
public sealed record Coding
{
    /// <summary>Identity of the terminology system.</summary>
    /// <remarks>Eg. "http://snomed.info/sct".</remarks>
    public string? System { get; init; }

    /// <summary>Symbol in syntax defined by the system.</summary>
    public string? Code { get; init; }

    /// <summary>Representation defined by the system.</summary>
    public string? Display { get; init; }
}
