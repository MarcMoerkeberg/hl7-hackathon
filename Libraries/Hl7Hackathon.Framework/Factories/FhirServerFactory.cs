using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hl7Hackathon.Framework.Factories;

public class FhirServerFactory : IFhirServerFactory
{
    private readonly ServerConnection _fhirServer;
    private readonly IFhirService _fhirResourceService;
    private readonly ILogger<FhirServerFactory> _logger;

    public FhirServerFactory(
        IOptions<AppSettings> appSettings,
        IFhirService fhirResourceService,
        ILogger<FhirServerFactory> logger)
    {
        _fhirServer = appSettings.Value.FhirServer;
        _fhirResourceService = fhirResourceService;
        _logger = logger;
    }

    public async Task<Result<FhirServerInfoModel>> GetFhirServerInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var capabilityStatement = await _fhirResourceService.GetCapabilityStatementAsync(cancellationToken);
            if (capabilityStatement.IsFailure)
            {
                _logger.LogWarning("Getting metadata from FHIR server {ServerName} failed: {Error}", _fhirServer.Name, capabilityStatement.Error);
                return capabilityStatement.Error;
            }

            return FhirServerMapper.ToInfoModel(_fhirServer, capabilityStatement.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata from FHIR server {ServerName}", _fhirServer.Name);
            return Error.Unexpected($"An unexpected error occurred while contacting FHIR server '{_fhirServer.Name}': {ex.Message}");
        }
    }
}
