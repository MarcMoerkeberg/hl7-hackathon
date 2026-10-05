using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hl7Hackathon.Framework.Factories;

public class FhirExchangeFactory : IFhirExchangeFactory
{
    public const int MaxCount = 500;

    private readonly IFhirExchangeService _fhirExchangeService;
    private readonly ILogger<FhirExchangeFactory> _logger;

    public FhirExchangeFactory(IFhirExchangeService fhirExchangeService, ILogger<FhirExchangeFactory> logger)
    {
        _fhirExchangeService = fhirExchangeService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<FhirExchangeSummaryModel>>> GetRecentFhirExchangesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (count is < 1 or > MaxCount)
            {
                return Error.Validation($"Count must be between 1 and {MaxCount}.");
            }

            var fhirExchanges = await _fhirExchangeService.GetRecentFhirExchangesAsync(count, cancellationToken);
            var models = fhirExchanges.Select(FhirExchangeMapper.ToSummaryModel).ToList();

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting the {Count} most recent FHIR exchanges", count);
            return Error.Unexpected("An unexpected error occurred while getting the FHIR exchange log.");
        }
    }

    public async Task<Result<FhirExchangeModel>> GetFhirExchangeByIdAsync(long fhirExchangeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var fhirExchange = await _fhirExchangeService.GetFhirExchangeByIdAsync(fhirExchangeId, cancellationToken);
            if (fhirExchange is null)
            {
                return Error.NotFound($"FHIR exchange {fhirExchangeId} was not found.");
            }

            return FhirExchangeMapper.ToModel(fhirExchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FHIR exchange {FhirExchangeId}", fhirExchangeId);
            return Error.Unexpected("An unexpected error occurred while getting the FHIR exchange.");
        }
    }

    public async Task<Result<int>> ClearFhirExchangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _fhirExchangeService.DeleteAllFhirExchangesAsync(cancellationToken);
            _logger.LogInformation("Cleared {DeletedCount} FHIR exchanges", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing the FHIR exchange log");
            return Error.Unexpected("An unexpected error occurred while clearing the FHIR exchange log.");
        }
    }
}
