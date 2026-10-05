namespace Hl7Hackathon.Fhir.Common;

/// <summary>
/// Canonical URLs from the telemedicine IG.
/// </summary>
/// <remarks>See <see href="https://hl7.dk/fhir/hackathons/artifacts.html">the IG artifacts</see>.</remarks>
public static class Telemedicine
{
    public static class Profiles
    {
        public const string TelemedicinePatient = "http://hl7.dk/fhir/hackathons/StructureDefinition/TelemedicinePatient";
        public const string TelemedicineEpisodeOfCare = "http://hl7.dk/fhir/hackathons/StructureDefinition/TelemedicineEpisodeOfCare";
        public const string SleepObservation = "http://hl7.dk/fhir/hackathons/StructureDefinition/SleepObservation";
        public const string PhysicalActivityObservation = "http://hl7.dk/fhir/hackathons/StructureDefinition/PhysicalActivityObservation";
        public const string TelemedicineQuestionnaire = "http://hl7.dk/fhir/hackathons/StructureDefinition/TelemedicineQuestionnaire";
        public const string TelemedicineQuestionnaireResponse = "http://hl7.dk/fhir/hackathons/StructureDefinition/TelemedicineQuestionnaireResponse";
    }

    /// <summary>
    /// The extension that links an Observation or QuestionnaireResponse to its EpisodeOfCare.
    /// </summary>
    public const string EpisodeOfCareExtensionUrl = "http://hl7.org/fhir/StructureDefinition/workflow-episodeOfCare";

    /// <summary>
    /// The URI that names SNOMED CT as a code system in <c>Coding.system</c>.
    /// </summary>
    /// <remarks>See <see href="https://hl7.org/fhir/R4/terminologies-systems.html">the R4 list of code system URIs</see>.</remarks>
    public const string SnomedSystem = "http://snomed.info/sct";
}
