namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// One activity a physical activity observation can be about.
/// </summary>
public sealed record ActivityCodeModel
{
    /// <summary>The SNOMED CT code.</summary>
    public required string Code { get; init; }

    /// <summary>The name to show.</summary>
    public required string Display { get; init; }
}
