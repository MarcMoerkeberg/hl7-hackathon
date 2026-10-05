---
name: fhir-telemedicine-ig
description: Reference for the HL7 DK hackathon 2026 telemedicine IG - profiles, canonical URLs, required elements, SNOMED codes, the questionnaire, the day's task list and known gotchas. Use when building, mapping, validating or searching FHIR resources (Patient, EpisodeOfCare, Observation, QuestionnaireResponse, Questionnaire) in this repo.
---

# Telemedicine IG (hackathon 2026, track 2)

- FHIR R4 (4.0.1). IG canonical base: `http://hl7.dk/fhir/hackathons`. Depends on DK Core (`hl7.fhir.dk.core`).
- Published IG: https://hl7.dk/fhir/hackathons/ - source FSH: https://github.com/hl7dk/dk-hackathons/tree/main/input/fsh
- The IG was reused from the 2022 hackathon. Verify canonical URLs and codes against the server on the day
  (`GET [base]/StructureDefinition?url=...`, `GET [base]/Questionnaire`) before relying on them.
- The hackathon server has these profiles loaded and **validates on submit**. A profile violation comes back as an
  `OperationOutcome` (usually HTTP 400/422). Always set `meta.profile` to the matching profile canonical.

## Profiles

Profile canonical = `http://hl7.dk/fhir/hackathons/StructureDefinition/<Id>`.

| Id | Base | Constraints |
|---|---|---|
| `TelemedicinePatient` | DkCorePatient | `name` 1..* (MS); `identifier` 1..* (from DK Core). CPR identifier system `urn:oid:1.2.208.176.1.2` |
| `TelemedicineEpisodeOfCare` | EpisodeOfCare | `patient` = Reference(TelemedicinePatient), `period` 1..1 (set `period.start`), `status` |
| `SleepObservation` | Observation | `code` fixed `http://snomed.info/sct#248263006`; `subject` and `performer` (1..1) = the patient; `effectivePeriod` required; `valueQuantity` required, unit `"Hours"`; no `component`; extension `workflow-episodeOfCare` 1..1 |
| `PhysicalActivityObservation` | Observation | `code` from ValueSet `ActivityCodes` (extensible, SNOMED); `subject` and `performer` = patient; `effectivePeriod` required; `valueQuantity` unit `"Minutes"`; no `component`; extension `workflow-episodeOfCare` 1..1 |
| `TelemedicineQuestionnaireResponse` | QuestionnaireResponse | `questionnaire` 1..1 (canonical), `subject`, `authored`, `source` (Patient/Practitioner/RelatedPerson) all required; extension `workflow-episodeOfCare` 1..1 |
| `TelemedicineQuestionnaire` | Questionnaire | No extra constraints. Instance: `Questionnaire/MentalHealthQuestionnaire` |

The episode link extension on Observations and QuestionnaireResponses:

```json
"extension": [{
  "url": "http://hl7.org/fhir/StructureDefinition/workflow-episodeOfCare",
  "valueReference": { "reference": "EpisodeOfCare/<id>" }
}]
```

## Where the repo encodes this

- Profile canonicals and the extension URL: `Libraries/Hl7Hackathon.Fhir/Common/Telemedicine.cs`.
- What each kind of Observation fixes (profile, SNOMED code, unit, whether a value is required): `Fhir/Codes/ObservationKind.cs`.
- The constraints above are data annotations and `IValidatableObject` rules on the models in `Fhir/Models/`;
  `FhirService` checks them before sending, so most violations show up locally with the FHIR path of the element.
  The server can still reject what we don't model (e.g. the ActivityCodes value set, references to the right type).
- Resources that meet every profile, for tests: `Tests/Hl7Hackathon.TestHelpers/TestFhirResources.cs`.

## Codes

- Sleep: `http://snomed.info/sct#248263006` (Duration of sleep)
- Activity (`ActivityCodes` ValueSet): SNOMED descendants of `418305009` (self-propelled locomotion),
  `256235009` (exercise) and `68130003` (physical activity), plus `129016000` (climbing stairs).
  The IG example uses `129006008` "Walking".
- Questionnaire answers: CodeSystem `MentalHealthCodes` (the example uses
  `http://hl7.dk/fhir/Hackathon-Sep-2022/CodeSystem/MentalHealthCodes`, with GUID-like codes). Read the
  `Questionnaire` from the server and use the `answerOption`/`answerValueSet` it gives, instead of hard-coding codes.

## Questionnaire structure (linkIds)

- `1` Fællesskab: `1.1` Venskaber, `1.2` Lyst til at være sammen med andre, `1.3` Kærlighed
- `2` Daglige opgaver: `2.1` Arbejde/skole forventning, `2.2` Pres
- `3` Følelser: `3.1` Oplagthed, `3.2` Humør

Each leaf has three coded options (good / middle / bad). The response nests items as group -> question -> `answer.valueCoding`.

## Example: sleep observation

```json
{
  "resourceType": "Observation",
  "meta": { "profile": ["http://hl7.dk/fhir/hackathons/StructureDefinition/SleepObservation"] },
  "extension": [{
    "url": "http://hl7.org/fhir/StructureDefinition/workflow-episodeOfCare",
    "valueReference": { "reference": "EpisodeOfCare/123" }
  }],
  "status": "final",
  "code": { "coding": [{ "system": "http://snomed.info/sct", "code": "248263006" }] },
  "subject": { "reference": "Patient/456" },
  "performer": [{ "reference": "Patient/456" }],
  "effectivePeriod": { "start": "2026-10-05T12:00:00+02:00", "end": "2026-10-06T12:00:00+02:00" },
  "valueQuantity": { "value": 7.5, "unit": "Hours" }
}
```

## The day's task list

1. Create a telemedicine episode (EpisodeOfCare) for a patient. Either POST a transaction Bundle with Patient +
   EpisodeOfCare (`fullUrl: urn:uuid:...` references), or reference a Patient already on the server.
2. POST sleep and physical-activity Observations for three consecutive days.
3. On the last day, also POST a QuestionnaireResponse.
4. Search all data on the episode for the patient.
5. Search all physical-activity Observations for the patient.
6. Count the sleep Observations on the server and compute the average hours of sleep.
7. Find your QuestionnaireResponse, then have the server return the Questionnaire it answers.

## Gotchas

- **Task 4:** `workflow-episodeOfCare` is an extension, and plain FHIR has no search parameter for it. Check the
  server's CapabilityStatement for a custom SearchParameter; otherwise use `Patient/<id>/$everything` or search by
  patient and filter client-side.
- **Task 5:** use `Observation?subject=Patient/<id>&_profile=<PhysicalActivityObservation canonical>`, or filter by
  code, excluding sleep.
- **Task 6:** FHIR has no aggregation. `Observation?code=http://snomed.info/sct|248263006&_summary=count` returns
  `Bundle.total`. For the average, page through the results with `GetNextPageAsync` and compute it yourself.
- **Task 7:** `QuestionnaireResponse.questionnaire` is a canonical, so resolve it with `Questionnaire?url=<canonical>`,
  not a read by id.
- The Quantity `unit` is the fixed display text `"Hours"`/`"Minutes"`, not a UCUM `code`.
- Use conditional create (`If-None-Exist: identifier=urn:oid:1.2.208.176.1.2|<cpr>`) for patients to avoid duplicates.
