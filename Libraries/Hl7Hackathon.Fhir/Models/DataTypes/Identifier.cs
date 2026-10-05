namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A business identifier, such as a CPR number.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#Identifier">Identifier (R4)</see>.</remarks>
public sealed record Identifier
{
    /// <summary>The namespace for the identifier value.</summary>
    /// <remarks>Eg. "urn:oid:1.2.208.176.1.2" for a CPR number.</remarks>
    public string? System { get; init; }

    /// <summary>The value that is unique.</summary>
    public string? Value { get; init; }
}
