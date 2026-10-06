using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IEpisodeOfCareFactory
{
    /// <summary>
    /// Opens a telemedicine course on the FHIR server for a patient in the local database.
    /// </summary>
    /// <param name="request">The patient, status and period of the course.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the created <see cref="EpisodeOfCareModel"/> if successful.
    /// </returns>
    Task<Result<EpisodeOfCareModel>> CreateEpisodeOfCareAsync(
        CreateEpisodeOfCareModel request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the episodes of care a patient in the local database has on the FHIR server.
    /// </summary>
    /// <param name="patientId">The id of the patient in the local database.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the patient's episodes of care if successful.
    /// </returns>
    Task<Result<IReadOnlyList<EpisodeOfCareModel>>> GetEpisodesOfCareForPatientAsync(
        long patientId,
        CancellationToken cancellationToken = default);
}
