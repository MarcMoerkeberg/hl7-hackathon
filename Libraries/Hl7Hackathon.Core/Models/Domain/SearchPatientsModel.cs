namespace Hl7Hackathon.Core.Models.Domain;

/// <summary>
/// What to look for when searching patients on the FHIR server. At least one criterion is needed.
/// </summary>
public sealed record SearchPatientsModel
{
    /// <summary>A CPR number, with or without its dash.</summary>
    public string? Cpr { get; init; }

    /// <summary>Any part of the patient's name.</summary>
    public string? Name { get; init; }

    /// <summary>The patient's date of birth.</summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>How many matches to ask the server for on one page.</summary>
    public int MaxResults { get; init; } = 20;
}
