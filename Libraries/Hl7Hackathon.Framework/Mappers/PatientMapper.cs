using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Models.Entities;

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
}
