using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;

namespace Hl7Hackathon.Framework.Mappers;

internal static class ObservationMapper
{
    /// <summary>
    /// Finds the IG profile for the <paramref name="kind"/> the UI asked for.
    /// </summary>
    /// <param name="kind">The kind to translate.</param>
    /// <returns>The kind's profile, code and unit, or <see langword="null"/> when it is not one we can send.</returns>
    public static ObservationKind? ToFhirKind(ObservationKindModel kind) => kind switch
    {
        ObservationKindModel.Sleep => ObservationKind.Sleep,
        ObservationKindModel.PhysicalActivity => ObservationKind.PhysicalActivity,
        _ => null
    };

    /// <summary>Every activity a physical activity observation can be about.</summary>
    public static IReadOnlyList<ActivityCodeModel> ToActivityCodeModels()
    {
        return ActivityCodes.All
            .Select(coding => new ActivityCodeModel { Code = coding.Code!, Display = coding.Display! })
            .ToList();
    }

    public static Observation ToFhir(
        CreateObservationModel request,
        ObservationKind kind,
        Coding code,
        string patientReference,
        string episodeOfCareReference) => new()
    {
        Meta = new Meta { Profile = [kind.Profile] },
        Extension =
        [
            new Extension
            {
                Url = Telemedicine.EpisodeOfCareExtensionUrl,
                ValueReference = new ResourceReference { Reference = episodeOfCareReference }
            }
        ],
        // The patient reports a measurement that is done and will not change.
        Status = ObservationStatus.Final,
        Code = new CodeableConcept { Coding = [code] },
        Subject = new ResourceReference { Reference = patientReference },
        Performer = [new ResourceReference { Reference = patientReference }],
        EffectivePeriod = new Period
        {
            Start = FhirDates.ToDateTime(request.Start),
            End = FhirDates.ToDateTime(request.End)
        },
        ValueQuantity = request.Value is { } value
            ? new Quantity { Value = value, Unit = kind.Unit }
            : null
    };

    public static ObservationModel ToModel(Observation observation)
    {
        var coding = observation.Code.Coding?.FirstOrDefault();

        return new ObservationModel
        {
            // The caller only maps an observation the server returned with an id.
            Id = observation.Id!,
            Status = observation.Status.Code,
            Kind = ToKindModel(ObservationKind.FromClaimedProfile(observation)),
            Code = coding?.Display ?? coding?.Code ?? observation.Code.Text,
            PatientReference = observation.Subject?.Reference,
            EpisodeOfCareReference = observation
                .GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl)
                .FirstOrDefault()?.ValueReference?.Reference,
            Start = observation.EffectivePeriod?.Start,
            End = observation.EffectivePeriod?.End,
            Value = observation.ValueQuantity?.Value,
            Unit = observation.ValueQuantity?.Unit
        };
    }

    private static ObservationKindModel? ToKindModel(ObservationKind? kind)
    {
        if (kind is null)
        {
            return null;
        }

        return kind.Profile switch
        {
            Telemedicine.Profiles.SleepObservation => ObservationKindModel.Sleep,
            Telemedicine.Profiles.PhysicalActivityObservation => ObservationKindModel.PhysicalActivity,
            _ => null
        };
    }
}
