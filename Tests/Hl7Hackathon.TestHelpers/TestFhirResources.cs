using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.QuestionnaireResponses;
using Hl7Hackathon.Fhir.Models.Questionnaires;

namespace Hl7Hackathon.TestHelpers;

/// <summary>
/// Resources that meet the telemedicine IG. Tests send them as they are, or break one element with <c>with</c>.
/// </summary>
public static class TestFhirResources
{
    public const string CprSystem = DkCore.CprSystem;
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

    public const string QuestionnaireUrl = "http://hl7.dk/fhir/hackathons/Questionnaire/MentalHealthQuestionnaire";
    public const string MentalHealthCodeSystem = "http://hl7.dk/fhir/Hackathon-Sep-2022/CodeSystem/MentalHealthCodes";

    /// <summary>
    /// A cut-down mental health questionnaire: one group with two coded questions, which is enough to exercise
    /// nesting, option lookup and partial answers.
    /// </summary>
    public static Questionnaire TelemedicineQuestionnaire(string url = QuestionnaireUrl) => new()
    {
        Meta = new Meta { Profile = [Telemedicine.Profiles.TelemedicineQuestionnaire] },
        Url = url,
        Title = "Mental health questionnaire",
        Status = "active",
        Item =
        [
            new QuestionnaireItem
            {
                LinkId = "1",
                Text = "Fællesskab",
                Type = "group",
                Item =
                [
                    new QuestionnaireItem
                    {
                        LinkId = "1.1",
                        Text = "Venskaber",
                        Type = "choice",
                        AnswerOption =
                        [
                            AnswerOption("60e95414-b97d-4197-af85-d5efc5628e86", "Jeg føler mig tæt på mine venner"),
                            AnswerOption("48405c96-5f94-4ccf-b9ef-44579da052a8", "Jeg har nogle få venner"),
                            AnswerOption("1a705260-cf44-4dd8-83fb-994baf656a6a", "Jeg føler mig alene")
                        ]
                    },
                    new QuestionnaireItem
                    {
                        LinkId = "1.2",
                        Text = "Kærlighed",
                        Type = "choice",
                        AnswerOption =
                        [
                            AnswerOption("a5341a86-677d-4d53-84a1-498dc1a4ed42", "Jeg føler mig elsket"),
                            AnswerOption("b9453f69-31a2-4a67-a453-569d7cd9691a", "Jeg føler mig ikke elsket")
                        ]
                    }
                ]
            }
        ]
    };

    private static QuestionnaireAnswerOption AnswerOption(string code, string display) => new()
    {
        ValueCoding = new Coding { System = MentalHealthCodeSystem, Code = code, Display = display }
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
