using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Fhir.Validation;

/// <summary>
/// Requires a string to be a FHIR <c>dateTime</c>: a (partial) date, or a date and time with a time zone.
/// </summary>
/// <remarks>The pattern is the one from <see href="https://hl7.org/fhir/R4/datatypes.html#dateTime">the R4 spec</see>.</remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FhirDateTimeAttribute : RegularExpressionAttribute
{
    private const string DateTimePattern = @"([0-9]([0-9]([0-9][1-9]|[1-9]0)|[1-9]00)|[1-9]000)(-(0[1-9]|1[0-2])(-(0[1-9]|[1-2][0-9]|3[0-1])(T([01][0-9]|2[0-3]):[0-5][0-9]:([0-5][0-9]|60)(\.[0-9]+)?(Z|(\+|-)((0[0-9]|1[0-3]):[0-5][0-9]|14:00)))?)?)?";

    public FhirDateTimeAttribute()
        : base(DateTimePattern)
    {
        ErrorMessage = "The {0} field must be a FHIR dateTime (YYYY, YYYY-MM, YYYY-MM-DD or YYYY-MM-DDThh:mm:ss+zz:zz).";
    }
}
