using System.Globalization;

namespace Hl7Hackathon.Web.Common;

/// <summary>
/// Formats values the way they are written in Denmark.
/// </summary>
/// <remarks>
/// Display only. What goes to the FHIR server keeps the formats the spec requires, see the mappers in
/// <c>Hl7Hackathon.Framework</c>.
/// </remarks>
public static class DisplayFormat
{
    /// <summary>The Danish date pattern, for the date pickers and the tables alike.</summary>
    public const string DatePattern = "dd-MM-yyyy";

    private const string DateTimePattern = "dd-MM-yyyy HH:mm";

    public static string Date(DateOnly date) => date.ToString(DatePattern, CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a FHIR <c>date</c> for display.
    /// </summary>
    /// <param name="value">The date as the server wrote it, eg. "1948-12-25".</param>
    /// <returns>
    /// The date in Danish order, or the value unchanged when it is partial ("1948-12") and has no day to show.
    /// </returns>
    public static string? FhirDate(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? Date(date)
            : value;
    }

    /// <summary>
    /// Formats a FHIR <c>dateTime</c> for display, in the time zone it was written in.
    /// </summary>
    /// <param name="value">The instant as the server wrote it, eg. "2026-10-06T00:00:00+02:00".</param>
    /// <returns>The date and time of day in Danish order, or whatever <see cref="FhirDate"/> makes of a value without a time.</returns>
    public static string? FhirDateTime(string? value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment)
            ? moment.ToString(DateTimePattern, CultureInfo.InvariantCulture)
            : FhirDate(value);
    }

    /// <summary>
    /// Writes a CPR number the Danish way, with a dash before the last four digits.
    /// </summary>
    /// <param name="cpr">The CPR number as it is stored, which is ten digits without a dash.</param>
    /// <returns>The CPR number as "DDMMYY-SSSS", or unchanged when it is not ten digits after all.</returns>
    public static string? Cpr(string? cpr)
    {
        return cpr?.Length == 10 && cpr.All(char.IsAsciiDigit)
            ? $"{cpr[..6]}-{cpr[6..]}"
            : cpr;
    }
}
