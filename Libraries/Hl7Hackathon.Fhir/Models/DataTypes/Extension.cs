namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// Additional content defined by implementations, identified by its <see cref="Url"/>.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.org/fhir/R4/extensibility.html#Extension">Extension (R4)</see>. The IG uses
/// <see href="https://hl7.org/fhir/R4/extension-workflow-episodeofcare.html">workflow-episodeOfCare</see>.
/// </remarks>
public sealed record Extension
{
    /// <summary>Identifies the meaning of the extension.</summary>
    /// <remarks>Eg. "http://hl7.org/fhir/StructureDefinition/workflow-episodeOfCare".</remarks>
    public required string Url { get; init; }

    /// <summary>Value of extension, as a reference.</summary>
    /// <remarks>value[x] holds one value of one type; add the other variants when an extension needs them.</remarks>
    public ResourceReference? ValueReference { get; init; }
}
