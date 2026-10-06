using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Microsoft.Extensions.Logging;

namespace Hl7Hackathon.Framework.Factories;

public class ObservationFactory : IObservationFactory
{
    private readonly IFhirService _fhirService;
    private readonly ILogger<ObservationFactory> _logger;

    public ObservationFactory(IFhirService fhirService, ILogger<ObservationFactory> logger)
    {
        _fhirService = fhirService;
        _logger = logger;
    }

    public IReadOnlyList<ActivityCodeModel> GetActivityCodes() => ObservationMapper.ToActivityCodeModels();

    public async Task<Result<ObservationModel>> CreateObservationAsync(
        CreateObservationModel request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ObservationMapper.ToFhirKind(request.Kind) is not { } kind)
            {
                return Error.Validation($"'{request.Kind}' is not a kind of observation that can be reported.");
            }

            if (request.End < request.Start)
            {
                return Error.Validation("The period cannot end before it starts.");
            }

            if (kind.IsValueRequired && request.Value is null)
            {
                return Error.Validation($"A value in {kind.Unit.ToLowerInvariant()} is required for this kind of observation.");
            }

            if (request.Value is { } value && value < 0)
            {
                return Error.Validation("The value cannot be negative.");
            }

            // A kind with no fixed code takes it from the ActivityCodes value set instead.
            var code = kind.Code ?? ActivityCodes.FromCode(request.ActivityCode);
            if (code is null)
            {
                return Error.Validation("Choose the activity the observation is about.");
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

            var observation = ObservationMapper.ToFhir(
                request,
                kind,
                code,
                patientReference,
                $"EpisodeOfCare/{request.EpisodeOfCareId}");

            var created = await _fhirService.CreateResourceAsync(observation, cancellationToken: cancellationToken);
            if (created.IsFailure)
            {
                _logger.LogWarning(
                    "Creating a {Kind} observation on episode of care {EpisodeOfCareId} failed: {Error}",
                    request.Kind,
                    request.EpisodeOfCareId,
                    created.Error);

                return created.Error;
            }

            if (created.Value.Id is null)
            {
                return Error.Unexpected("The FHIR server accepted the observation but did not return it with an id.");
            }

            return ObservationMapper.ToModel(created.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating a {Kind} observation", request.Kind);
            return Error.Unexpected("An unexpected error occurred while creating the observation.");
        }
    }
}
