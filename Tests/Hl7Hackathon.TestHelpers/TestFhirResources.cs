using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;

namespace Hl7Hackathon.TestHelpers;

/// <summary>
/// Resources that meet the telemedicine IG. Tests send them as they are, or break one element with <c>with</c>.
/// </summary>
public static class TestFhirResources
{
    public const string CprSystem = "urn:oid:1.2.208.176.1.2";
    public const string PatientReference = "Patient/1";
    public const string EpisodeOfCareReference = "EpisodeOfCare/1";

    public static Patient TelemedicinePatient(string cpr = "2512489996") => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicinePatient] },
        Identifier = [new Identifier { System = CprSystem, Value = cpr }],
        Name = [new HumanName { Use = NameUse.Official, Family = "Berggren", Given = ["Nancy", "Ann"] }],
        Gender = AdministrativeGender.Female,
        BirthDate = "1948-12-25"
    };

    public static EpisodeOfCare TelemedicineEpisodeOfCare(string patientReference = PatientReference) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicineEpisodeOfCare] },
        Status = EpisodeOfCareStatus.Active,
        Patient = new ResourceReference { Reference = patientReference },
        Period = new Period { Start = "2026-10-06" }
    };

    public static Observation SleepObservation(
        string patientReference = PatientReference,
        string episodeOfCareReference = EpisodeOfCareReference) => new()
    {
        Meta = new Meta { Profile = [ObservationKind.Sleep.Profile] },
        Extension = [EpisodeOfCareExtension(episodeOfCareReference)],
        Status = ObservationStatus.Final,
        Code = new CodeableConcept { Coding = [ObservationKind.Sleep.Code!] },
        Subject = new ResourceReference { Reference = patientReference },
        Performer = [new ResourceReference { Reference = patientReference }],
        EffectivePeriod = new Period { Start = "2026-10-05T12:00:00+02:00", End = "2026-10-06T12:00:00+02:00" },
        ValueQuantity = new Quantity { Value = 7.5m, Unit = ObservationKind.Sleep.Unit }
    };

    public static Observation PhysicalActivityObservation(
        string patientReference = PatientReference,
        string episodeOfCareReference = EpisodeOfCareReference) => new()
    {
        Meta = new Meta { Profile = [ObservationKind.PhysicalActivity.Profile] },
        Extension = [EpisodeOfCareExtension(episodeOfCareReference)],
        Status = ObservationStatus.Final,
        Code = new CodeableConcept { Coding = [new Coding { System = Telemedicine.SnomedSystem, Code = "129006008", Display = "Walking" }] },
        Subject = new ResourceReference { Reference = patientReference },
        Performer = [new ResourceReference { Reference = patientReference }],
        EffectivePeriod = new Period { Start = "2026-10-05T12:00:00+02:00", End = "2026-10-06T12:00:00+02:00" },
        ValueQuantity = new Quantity { Value = 45, Unit = ObservationKind.PhysicalActivity.Unit }
    };

    public static QuestionnaireResponse TelemedicineQuestionnaireResponse(
        string patientReference = PatientReference,
        string episodeOfCareReference = EpisodeOfCareReference) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicineQuestionnaireResponse] },
        Extension = [EpisodeOfCareExtension(episodeOfCareReference)],
        Questionnaire = "http://hl7.dk/fhir/hackathons/Questionnaire/MentalHealthQuestionnaire",
        Status = QuestionnaireResponseStatus.Completed,
        Subject = new ResourceReference { Reference = patientReference },
        Authored = "2026-10-06T09:00:00+02:00",
        Source = new ResourceReference { Reference = patientReference },
        Item =
        [
            new QuestionnaireResponseItem
            {
                LinkId = "1",
                Item = [new QuestionnaireResponseItem { LinkId = "1.1", Answer = [new QuestionnaireResponseAnswer { ValueCoding = new Coding { Code = "good" } }] }]
            }
        ]
    };

    public static Extension EpisodeOfCareExtension(string episodeOfCareReference = EpisodeOfCareReference) => new()
    {
        Url = Telemedicine.EpisodeOfCareExtensionUrl,
        ValueReference = new ResourceReference { Reference = episodeOfCareReference }
    };
}
