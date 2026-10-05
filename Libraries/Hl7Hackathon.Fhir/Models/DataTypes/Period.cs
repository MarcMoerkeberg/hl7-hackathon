using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.Fhir.Models.DataTypes;

/// <summary>
/// A time period defined by a start and end date/time.
/// </summary>
/// <remarks>
/// Contract: <see href="https://hl7.org/fhir/R4/datatypes.html#Period">Period (R4)</see>. The bounds are strings
/// because a FHIR dateTime may be partial ("2026" or "2026-10"), which DateTimeOffset cannot hold.
/// </remarks>
public sealed record Period
{
    /// <summary>Starting time with inclusive boundary.</summary>
    [FhirDateTime]
    public string? Start { get; init; }

    /// <summary>End time with inclusive boundary, if not ongoing.</summary>
    [FhirDateTime]
    public string? End { get; init; }
}
