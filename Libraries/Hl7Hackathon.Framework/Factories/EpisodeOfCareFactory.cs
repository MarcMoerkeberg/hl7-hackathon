using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hl7Hackathon.Framework.Factories;

public class EpisodeOfCareFactory : IEpisodeOfCareFactory
{
    private readonly IPatientService _patientService;
    private readonly IFhirService _fhirService;
    private readonly ILogger<EpisodeOfCareFactory> _logger;

    public EpisodeOfCareFactory(
        IPatientService patientService,
        IFhirService fhirService,
        ILogger<EpisodeOfCareFactory> logger)
    {
        _patientService = patientService;
        _fhirService = fhirService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EpisodeOfCareModel>>> GetEpisodesOfCareForPatientAsync(
        long patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId, cancellationToken);
            if (patient is null)
            {
                return Error.NotFound($"Patient {patientId} was not found.");
            }

            var searchResult = await _fhirService.SearchResourcesAsync<EpisodeOfCare>(
                EpisodeOfCareMapper.ToPatientSearchParameters(patient),
                cancellationToken);
            if (searchResult.IsFailure)
            {
                _logger.LogWarning(
                    "Searching episodes of care for patient {PatientId} failed: {Error}",
                    patientId,
                    searchResult.Error);

                return searchResult.Error;
            }

            // A searchset can also carry included resources and an OperationOutcome, so only episodes are mapped.
            var episodesOfCare = searchResult.Value.Entry
                ?.Select(entry => entry.Resource)
                .OfType<EpisodeOfCare>()
                .Where(episodeOfCare => episodeOfCare.Id is not null)
                .Select(EpisodeOfCareMapper.ToModel)
                .ToList() ?? [];

            return episodesOfCare;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching episodes of care for patient {PatientId}", patientId);
            return Error.Unexpected("An unexpected error occurred while getting the episodes of care.");
        }
    }

    public async Task<Result<EpisodeOfCareModel>> CreateEpisodeOfCareAsync(
        CreateEpisodeOfCareModel request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (EpisodeOfCareMapper.ToFhirStatus(request.Status) is not { } status)
            {
                return Error.Validation($"'{request.Status}' is not a status an episode of care can be created with.");
            }

            if (request.End is { } end && end < request.Start)
            {
                return Error.Validation("The episode of care cannot end before it starts.");
            }

            var patient = await _patientService.GetPatientByIdAsync(request.PatientId, cancellationToken);
            if (patient is null)
            {
                return Error.NotFound($"Patient {request.PatientId} was not found.");
            }

            var transaction = EpisodeOfCareMapper.ToCreateTransaction(request, status, patient);
            var transactionResult = await _fhirService.PostTransactionAsync(transaction, cancellationToken);
            if (transactionResult.IsFailure)
            {
                _logger.LogWarning(
                    "Creating an episode of care for patient {PatientId} failed: {Error}",
                    request.PatientId,
                    transactionResult.Error);

                return transactionResult.Error;
            }

            var createdEpisodeOfCare = transactionResult.Value.Entry
                ?.Select(entry => entry.Resource)
                .OfType<EpisodeOfCare>()
                .FirstOrDefault();
            if (createdEpisodeOfCare?.Id is null)
            {
                return Error.Unexpected("The FHIR server accepted the transaction but did not return the created episode of care.");
            }

            return EpisodeOfCareMapper.ToModel(createdEpisodeOfCare);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating an episode of care for patient {PatientId}", request.PatientId);
            return Error.Unexpected("An unexpected error occurred while creating the episode of care.");
        }
    }
}
