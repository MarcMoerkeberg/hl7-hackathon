using Hl7Hackathon.Core.Models.Entities;

namespace Hl7Hackathon.Services.Interfaces;

public interface IPatientService
{
    Task<IReadOnlyList<Patient>> GetAllPatientsAsync(CancellationToken cancellationToken = default);

    Task<Patient?> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default);
}
