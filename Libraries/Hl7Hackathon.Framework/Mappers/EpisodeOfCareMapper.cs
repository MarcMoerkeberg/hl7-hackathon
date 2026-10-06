using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.DataTypes;
using PatientEntity = Hl7Hackathon.Core.Models.Entities.Patient;

namespace Hl7Hackathon.Framework.Mappers;

internal static class EpisodeOfCareMapper
{
    private const string PatientResourceType = "Patient";
    private const string EpisodeOfCareResourceType = "EpisodeOfCare";

    /// <summary>
    /// Finds the FHIR code for the <paramref name="status"/> the UI asked for.
    /// </summary>
    /// <param name="status">The status to translate.</param>
    /// <returns>The FHIR code, or <see langword="null"/> when the status is not one we can send.</returns>
    public static EpisodeOfCareStatus? ToFhirStatus(EpisodeOfCareStatusModel status) => status switch
    {
        EpisodeOfCareStatusModel.Planned => EpisodeOfCareStatus.Planned,
        EpisodeOfCareStatusModel.Waitlist => EpisodeOfCareStatus.Waitlist,
        EpisodeOfCareStatusModel.Active => EpisodeOfCareStatus.Active,
        EpisodeOfCareStatusModel.OnHold => EpisodeOfCareStatus.OnHold,
        EpisodeOfCareStatusModel.Finished => EpisodeOfCareStatus.Finished,
        EpisodeOfCareStatusModel.Cancelled => EpisodeOfCareStatus.Cancelled,
        _ => null
    };

    /// <summary>
    /// Builds the transaction that puts the patient and the episode of care on the server in one request.
    /// </summary>
    /// <param name="request">What the UI asked for.</param>
    /// <param name="status">The FHIR status of the episode of care.</param>
    /// <param name="patient">The patient from the local database the episode of care is for.</param>
    /// <returns>A transaction <see cref="Bundle"/> ready to post to the server's base URL.</returns>
    public static Bundle ToCreateTransaction(CreateEpisodeOfCareModel request, EpisodeOfCareStatus status, PatientEntity patient)
    {
        // The patient has no id yet, so it gets a placeholder the episode of care can reference; the server rewrites both.
        var patientFullUrl = $"urn:uuid:{Guid.NewGuid()}";

        return new Bundle
        {
            Type = BundleType.Transaction,
            Entry =
            [
                new BundleEntry
                {
                    FullUrl = patientFullUrl,
                    Resource = PatientMapper.ToFhir(patient),
                    Request = new BundleEntryRequest
                    {
                        Method = HttpVerb.Post,
                        Url = PatientResourceType,
                        // Reuses the patient already on the server when someone else created them first.
                        IfNoneExist = PatientMapper.ToCprSearch(patient)
                    }
                },
                new BundleEntry
                {
                    Resource = ToFhir(request, status, patientFullUrl),
                    Request = new BundleEntryRequest { Method = HttpVerb.Post, Url = EpisodeOfCareResourceType }
                }
            ]
        };
    }

    /// <summary>
    /// Builds the search that finds every episode of care the patient has on the server.
    /// </summary>
    /// <param name="patient">The patient from the local database to find episodes for.</param>
    /// <returns>
    /// A chained search on the patient's CPR number, which saves looking up their id on the server first.
    /// </returns>
    public static IReadOnlyList<(string Name, string Value)> ToPatientSearchParameters(PatientEntity patient) =>
        [("patient.identifier", PatientMapper.ToCprToken(patient))];

    public static EpisodeOfCareModel ToModel(EpisodeOfCare episodeOfCare) => new()
    {
        // The caller only maps an episode of care the server returned with an id.
        Id = episodeOfCare.Id!,
        Status = episodeOfCare.Status.Code,
        PatientReference = episodeOfCare.Patient.Reference,
        Start = episodeOfCare.Period?.Start,
        End = episodeOfCare.Period?.End
    };

    private static EpisodeOfCare ToFhir(CreateEpisodeOfCareModel request, EpisodeOfCareStatus status, string patientReference) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicineEpisodeOfCare] },
        Status = status,
        Patient = new ResourceReference { Reference = patientReference },
        Period = new Period
        {
            Start = FhirDates.ToDateTime(request.Start),
            End = request.End is { } end ? FhirDates.ToDateTime(end) : null
        }
    };
}
