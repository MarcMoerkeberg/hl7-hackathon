namespace Hl7Hackathon.Fhir.Codes;

/// <summary>
/// The gender of a person used for administrative purposes.
/// </summary>
/// <remarks>Value set: <see href="https://hl7.org/fhir/R4/valueset-administrative-gender.html">administrative-gender (R4)</see>.</remarks>
public sealed record AdministrativeGender : FhirCode<AdministrativeGender>
{
    /// <summary>Male.</summary>
    public static readonly AdministrativeGender Male = new("male");

    /// <summary>Female.</summary>
    public static readonly AdministrativeGender Female = new("female");

    /// <summary>Other.</summary>
    public static readonly AdministrativeGender Other = new("other");

    /// <summary>Unknown.</summary>
    public static readonly AdministrativeGender Unknown = new("unknown");

    private AdministrativeGender(string code)
        : base(code)
    {
    }
}
