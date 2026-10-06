using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Models.Questionnaires;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Microsoft.Extensions.Logging;

namespace Hl7Hackathon.Framework.Factories;

public class QuestionnaireFactory : IQuestionnaireFactory
{
    private readonly IFhirService _fhirService;
    private readonly ILogger<QuestionnaireFactory> _logger;

    public QuestionnaireFactory(IFhirService fhirService, ILogger<QuestionnaireFactory> logger)
    {
        _fhirService = fhirService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<QuestionnaireModel>>> GetQuestionnairesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var searchResult = await _fhirService.SearchResourcesAsync<Questionnaire>([], cancellationToken);
            if (searchResult.IsFailure)
            {
                _logger.LogWarning("Searching questionnaires on the FHIR server failed: {Error}", searchResult.Error);
                return searchResult.Error;
            }

            // A searchset can also carry included resources and an OperationOutcome, so only questionnaires are
            // mapped, and only those a response can point at: that is by canonical URL, not by id.
            var questionnaires = searchResult.Value.Entry
                ?.Select(entry => entry.Resource)
                .OfType<Questionnaire>()
                .Where(questionnaire => questionnaire.Id is not null && questionnaire.Url is not null)
                .Select(QuestionnaireMapper.ToModel)
                .ToList() ?? [];

            return questionnaires;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching questionnaires on the FHIR server");
            return Error.Unexpected("An unexpected error occurred while getting the questionnaires.");
        }
    }

    public async Task<Result<QuestionnaireResponseModel>> CreateQuestionnaireResponseAsync(
        CreateQuestionnaireResponseModel request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.Answers.Count == 0)
            {
                return Error.Validation("Answer at least one question before sending the questionnaire.");
            }

            var questionnaireResult = await FindQuestionnaireAsync(request.QuestionnaireUrl, cancellationToken);
            if (questionnaireResult.IsFailure)
            {
                return questionnaireResult.Error;
            }

            var questionnaire = questionnaireResult.Value;

            // The answers carry the questionnaire's own codings, so this app never invents a code or a system.
            var answers = new Dictionary<string, Coding>(StringComparer.Ordinal);
            foreach (var answer in request.Answers)
            {
                if (QuestionnaireMapper.FindAnswerOption(questionnaire, answer.LinkId, answer.Code) is not { } coding)
                {
                    return Error.Validation($"'{answer.Code}' is not an answer the questionnaire allows to question '{answer.LinkId}'.");
                }

                answers[answer.LinkId] = coding;
            }

            var episodeOfCare = await _fhirService.GetResourceByIdAsync<EpisodeOfCare>(request.EpisodeOfCareId, cancellationToken);
            if (episodeOfCare.IsFailure)
            {
                return episodeOfCare.Error;
            }

            if (episodeOfCare.Value.Patient.Reference is not { } patientReference)
            {
                return Error.Validation($"Episode of care {request.EpisodeOfCareId} names no patient to report for.");
            }

            var response = QuestionnaireMapper.ToFhir(
                request,
                questionnaire,
                answers,
                patientReference,
                $"EpisodeOfCare/{request.EpisodeOfCareId}");

            var created = await _fhirService.CreateResourceAsync(response, cancellationToken: cancellationToken);
            if (created.IsFailure)
            {
                _logger.LogWarning(
                    "Creating a questionnaire response on episode of care {EpisodeOfCareId} failed: {Error}",
                    request.EpisodeOfCareId,
                    created.Error);

                return created.Error;
            }

            if (created.Value.Id is null)
            {
                return Error.Unexpected("The FHIR server accepted the questionnaire response but did not return it with an id.");
            }

            return QuestionnaireMapper.ToModel(created.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating a questionnaire response on episode of care {EpisodeOfCareId}", request.EpisodeOfCareId);
            return Error.Unexpected("An unexpected error occurred while creating the questionnaire response.");
        }
    }

    /// <summary>
    /// Finds the questionnaire a response points at.
    /// </summary>
    /// <remarks>
    /// <c>questionnaire</c> is a canonical URL rather than a reference, so it is resolved by searching on
    /// <c>url</c> instead of reading an id.
    /// </remarks>
    private async Task<Result<Questionnaire>> FindQuestionnaireAsync(string url, CancellationToken cancellationToken)
    {
        var searchResult = await _fhirService.SearchResourcesAsync<Questionnaire>([("url", url)], cancellationToken);
        if (searchResult.IsFailure)
        {
            return searchResult.Error;
        }

        var questionnaire = searchResult.Value.Entry
            ?.Select(entry => entry.Resource)
            .OfType<Questionnaire>()
            .FirstOrDefault(candidate => candidate.Url == url);

        return questionnaire is null
            ? Error.NotFound($"The FHIR server has no questionnaire with the URL '{url}'.")
            : questionnaire;
    }
}
