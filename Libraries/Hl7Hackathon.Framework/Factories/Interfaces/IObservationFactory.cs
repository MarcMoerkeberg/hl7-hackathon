using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IObservationFactory
{
    /// <summary>
    /// The activities a physical activity observation can be about.
    /// </summary>
    /// <remarks>
    /// A value set rather than an outcome, so it is not a <see cref="Result"/>: the list is fixed and cannot fail.
    /// </remarks>
    IReadOnlyList<ActivityCodeModel> GetActivityCodes();

    /// <summary>
    /// Reports a measurement on an episode of care that is already on the FHIR server.
    /// </summary>
    /// <param name="request">The episode of care, the kind of measurement, its period and its value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the created <see cref="ObservationModel"/> if successful.
    /// </returns>
    Task<Result<ObservationModel>> CreateObservationAsync(
        CreateObservationModel request,
        CancellationToken cancellationToken = default);
}
