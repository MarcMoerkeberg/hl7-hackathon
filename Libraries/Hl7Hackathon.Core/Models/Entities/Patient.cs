namespace Hl7Hackathon.Core.Models.Entities;

public class Patient
{
    public long Id { get; set; }

    public string Cpr { get; set; } = string.Empty;

    public string GivenName { get; set; } = string.Empty;

    public string FamilyName { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public DateTime BirthDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
