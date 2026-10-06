using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.Framework.Factories.Interfaces;

public interface IQuestionnaireFactory
{
    /// <summary>
    /// Gets the questionnaires the FHIR server publishes, with the questions and answers each of them allows.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the questionnaires if successful.
    /// </returns>
    Task<Result<IReadOnlyList<QuestionnaireModel>>> GetQuestionnairesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports a filled-in questionnaire on an episode of care that is already on the FHIR server.
    /// </summary>
    /// <param name="request">The episode of care, the questionnaire, when it was answered and the answers.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the created <see cref="QuestionnaireResponseModel"/> if successful.
    /// </returns>
    Task<Result<QuestionnaireResponseModel>> CreateQuestionnaireResponseAsync(
        CreateQuestionnaireResponseModel request,
        CancellationToken cancellationToken = default);
}
