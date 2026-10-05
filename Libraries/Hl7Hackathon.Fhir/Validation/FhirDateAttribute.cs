using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Validation;

/// <summary>
/// Requires a string to be a FHIR <c>date</c>: "YYYY", "YYYY-MM" or "YYYY-MM-DD", without a time or time zone.
/// </summary>
/// <remarks>The pattern is the one from <see href="https://hl7.org/fhir/R4/datatypes.html#date">the R4 spec</see>.</remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FhirDateAttribute : RegularExpressionAttribute
{
    private const string DatePattern = @"([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1]))?)?";

    public FhirDateAttribute()
        : base(DatePattern)
    {
        ErrorMessage = "The {0} field must be a FHIR date (YYYY, YYYY-MM or YYYY-MM-DD).";
    }
}
