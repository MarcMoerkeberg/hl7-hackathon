using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.DataTypes;
using FhirPatient = Hl7Hackathon.Fhir.Models.Patient;

namespace Hl7Hackathon.Framework.Mappers;

internal static class PatientMapper
{
    public static PatientModel ToModel(Patient patient) => new()
    {
        Id = patient.Id,
        Cpr = patient.Cpr,
        GivenName = patient.GivenName,
        FamilyName = patient.FamilyName,
        Gender = patient.Gender,
        BirthDate = DateOnly.FromDateTime(patient.BirthDate)
    };

    public static FhirPatientModel ToModel(FhirPatient patient)
    {
        // Other systems register several names; the official one is the one from the CPR register.
        var name = patient.Name?.FirstOrDefault(name => name.Use == NameUse.Official) ?? patient.Name?.FirstOrDefault();

        return new FhirPatientModel
        {
            // The caller only maps patients the server returned with an id.
            Id = patient.Id!,
            Cpr = patient.Identifier?.FirstOrDefault(identifier => identifier.System == DkCore.CprSystem)?.Value,
            GivenName = name?.Given is { } given ? string.Join(' ', given) : null,
            FamilyName = name?.Family,
            Gender = patient.Gender?.Code,
            BirthDate = patient.BirthDate
        };
    }

    public static FhirPatient ToFhir(Patient patient) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicinePatient] },
        Identifier = [new Identifier { System = DkCore.CprSystem, Value = patient.Cpr }],
        Name =
        [
            new HumanName
            {
                Use = NameUse.Official,
                Family = patient.FamilyName,
                Given = patient.GivenName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            }
        ],
        // The local record keeps the gender as free text; anything outside the FHIR value set becomes "unknown".
        Gender = AdministrativeGender.TryFromCode(patient.Gender, out var gender) ? gender : AdministrativeGender.Unknown,
        BirthDate = FhirDates.ToDate(DateOnly.FromDateTime(patient.BirthDate))
    };

    /// <summary>
    /// Builds the conditional create parameter that keeps the server from storing the same patient twice.
    /// </summary>
    /// <param name="patient">The patient to look for on the server.</param>
    /// <returns>An <c>If-None-Exist</c> search on the patient's CPR number.</returns>
    public static string ToCprSearch(Patient patient) => $"identifier={ToCprToken(patient)}";

    /// <summary>
    /// Builds the search token that identifies the patient by CPR number.
    /// </summary>
    /// <param name="patient">The patient to identify.</param>
    /// <returns>A "system|value" token for an identifier search parameter.</returns>
    public static string ToCprToken(Patient patient) => $"{DkCore.CprSystem}|{patient.Cpr}";

    /// <summary>
    /// Translates the criteria of a search into FHIR search parameters.
    /// </summary>
    /// <param name="request">The criteria to search by.</param>
    /// <returns>
    /// One parameter per criterion given, or an empty list when the request holds no criterion at all.
    /// </returns>
    public static IReadOnlyList<(string Name, string Value)> ToSearchParameters(SearchPatientsModel request)
    {
        var parameters = new List<(string Name, string Value)>();

        if (ToCprDigits(request.Cpr) is { } cpr)
        {
            parameters.Add(("identifier", $"{DkCore.CprSystem}|{cpr}"));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // "name" matches any part of any of the patient's names.
            parameters.Add(("name", request.Name.Trim()));
        }

        if (request.BirthDate is { } birthDate)
        {
            parameters.Add(("birthdate", FhirDates.ToDate(birthDate)));
        }

        return parameters;
    }

    // A CPR number is usually written with a dash, which the identifier on the server does not have.
    private static string? ToCprDigits(string? cpr)
    {
        var digits = new string((cpr ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

        return digits.Length == 0 ? null : digits;
    }
}
