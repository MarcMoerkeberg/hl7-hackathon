using System.Globalization;
using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging;
using FhirPatient = Hl7Hackathon.Fhir.Models.Patient;

namespace Hl7Hackathon.Framework.Factories;

public class PatientFactory : IPatientFactory
{
    // Keeps a mistyped page size from pulling the whole server, which is shared with everyone.
    private const int MinResults = 1;
    private const int MaxResults = 100;

    private readonly IPatientService _patientService;
    private readonly IFhirService _fhirService;
    private readonly ILogger<PatientFactory> _logger;

    public PatientFactory(IPatientService patientService, IFhirService fhirService, ILogger<PatientFactory> logger)
    {
        _patientService = patientService;
        _fhirService = fhirService;
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

    public async Task<Result<PatientSearchResultModel>> SearchPatientsAsync(
        SearchPatientsModel request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var criteria = PatientMapper.ToSearchParameters(request);
            if (criteria.Count == 0)
            {
                return Error.Validation("Search for a patient by CPR number, name or date of birth.");
            }

            var pageSize = Math.Clamp(request.MaxResults, MinResults, MaxResults);
            var searchResult = await _fhirService.SearchResourcesAsync<FhirPatient>(
                [.. criteria, ("_count", pageSize.ToString(CultureInfo.InvariantCulture))],
                cancellationToken);
            if (searchResult.IsFailure)
            {
                _logger.LogWarning("Searching patients on the FHIR server failed: {Error}", searchResult.Error);
                return searchResult.Error;
            }

            // A searchset can also carry included resources and an OperationOutcome, so only patients are mapped.
            var patients = searchResult.Value.Entry
                ?.Select(entry => entry.Resource)
                .OfType<FhirPatient>()
                .Where(patient => patient.Id is not null)
                .Select(patient => PatientMapper.ToModel(patient))
                .ToList() ?? [];

            return new PatientSearchResultModel { Patients = patients, Total = searchResult.Value.Total };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching patients on the FHIR server");
            return Error.Unexpected("An unexpected error occurred while searching for patients.");
        }
    }
}
