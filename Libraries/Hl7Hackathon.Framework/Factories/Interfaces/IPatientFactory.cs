using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IPatientFactory
{
    Task<Result<IReadOnlyList<PatientModel>>> GetPatientsAsync(CancellationToken cancellationToken = default);

    Task<Result<PatientModel>> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the FHIR server for patients matching the criteria in the <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The CPR number, name or date of birth to search by; at least one is needed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the matching page of patients if successful.
    /// </returns>
    Task<Result<PatientSearchResultModel>> SearchPatientsAsync(
        SearchPatientsModel request,
        CancellationToken cancellationToken = default);
}
