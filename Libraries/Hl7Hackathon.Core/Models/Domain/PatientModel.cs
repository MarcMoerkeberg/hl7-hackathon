namespace Hl7Hackathon.Core.Models.Domain;

public sealed record PatientModel
{
    public required long Id { get; init; }

    public required string Cpr { get; init; }

    public required string GivenName { get; init; }

    public required string FamilyName { get; init; }

    public required string Gender { get; init; }

    public required DateOnly BirthDate { get; init; }
}
