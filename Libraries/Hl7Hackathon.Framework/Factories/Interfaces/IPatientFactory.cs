using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IPatientFactory
{
    Task<Result<IReadOnlyList<PatientModel>>> GetPatientsAsync(CancellationToken cancellationToken = default);

    Task<Result<PatientModel>> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default);
}
