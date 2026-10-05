using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A measured amount, with its unit.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#Quantity">Quantity (R4)</see>.</remarks>
public sealed record Quantity : IValidatableObject
{
    /// <summary>Numerical value (with implicit precision).</summary>
    public decimal? Value { get; init; }

    /// <summary>Unit representation.</summary>
    /// <remarks>The IG fixes it to "Hours" for sleep and "Minutes" for physical activity.</remarks>
    public string? Unit { get; init; }

    /// <summary>System that defines coded unit form.</summary>
    /// <remarks>Normally UCUM ("http://unitsofmeasure.org").</remarks>
    public string? System { get; init; }

    /// <summary>Coded form of the unit.</summary>
    public string? Code { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Invariant qty-3.
        if (Code is not null && System is null)
        {
            yield return new ValidationResult("If a code for the unit is present, the system SHALL also be present.", [nameof(System)]);
        }
    }
}
