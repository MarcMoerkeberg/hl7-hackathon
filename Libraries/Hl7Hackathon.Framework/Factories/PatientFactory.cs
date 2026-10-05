using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hl7Hackathon.Framework.Factories;

public class PatientFactory : IPatientFactory
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientFactory> _logger;

    public PatientFactory(IPatientService patientService, ILogger<PatientFactory> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PatientModel>>> GetPatientsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var patients = await _patientService.GetAllPatientsAsync(cancellationToken);
            var models = patients.Select(PatientMapper.ToModel).ToList();

            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patients");
            return Error.Unexpected("An unexpected error occurred while getting the patients.");
        }
    }

    public async Task<Result<PatientModel>> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId, cancellationToken);
            if (patient is null)
            {
                return Error.NotFound($"Patient {patientId} was not found.");
            }

            return PatientMapper.ToModel(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient {PatientId}", patientId);
            return Error.Unexpected("An unexpected error occurred while getting the patient.");
        }
    }
}
