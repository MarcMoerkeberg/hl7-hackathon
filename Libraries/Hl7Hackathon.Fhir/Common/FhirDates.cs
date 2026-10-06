using System.Globalization;

namespace Hl7Hackathon.Fhir.Common;

/// <summary>
/// Writes the date formats FHIR puts on the wire.
/// </summary>
/// <remarks>
/// Always invariant: ":" is a culture specific separator in a custom format string, so a Danish culture would
/// otherwise write a time the server cannot parse.
/// </remarks>
public static class FhirDates
{
    private const string DateTimePattern = "yyyy-MM-ddTHH:mm:sszzz";
    private const string DatePattern = "yyyy-MM-dd";

    /// <summary>
    /// Writes a FHIR <c>dateTime</c>, keeping the offset so the server knows which wall clock the value is on.
    /// </summary>
    /// <param name="value">The instant to write.</param>
    /// <returns>Eg. "2026-10-06T08:30:00+02:00".</returns>
    public static string ToDateTime(DateTimeOffset value) => value.ToString(DateTimePattern, CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes a FHIR <c>date</c>, which carries no time and no time zone.
    /// </summary>
    /// <param name="value">The day to write.</param>
    /// <returns>Eg. "1948-12-25".</returns>
    public static string ToDate(DateOnly value) => value.ToString(DatePattern, CultureInfo.InvariantCulture);
}
