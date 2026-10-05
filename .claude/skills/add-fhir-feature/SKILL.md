---
name: add-fhir-feature
description: Step-by-step recipe for adding a FHIR use case end to end in this repo (DTO, FHIR mapping, service call, factory, DI, tests, UI) - e.g. "create an episode of care", "post a sleep observation", "search activity observations". Use when implementing one of the hackathon tasks or any new FHIR create/search flow.
---

# Adding a FHIR use case

Work from the inside out. Load `fhir-telemedicine-ig` for the profile rules and `code-guidelines` for style.

1. **DTOs** in `Libraries/Hl7Hackathon.Core/Models/Domain/`: a request model (what the UI sends, e.g.
   `CreateSleepObservationModel`) and a result model (what the UI shows, e.g. `SleepObservationModel` with the
   server id). Plain values only: strings, numbers, `DateTimeOffset`, `DateOnly`, enums. No FHIR types.

2. **FHIR models**: check `Libraries/Hl7Hackathon.Fhir/Models/` for the resource and every element the task needs, and
   add what is missing (see `code-guidelines` > FHIR): spec names, a contract link and short description per property,
   data annotations and `IValidatableObject` for the IG rules, a smart enum in `Fhir/Codes/` for a new required value
   set, and a `[JsonDerivedType]` on `Resource` for a new resource.

3. **Mapping** in `Libraries/Hl7Hackathon.Framework/Mappers/{Area}Mapper.cs` (`internal static`):
   - `ToFhir(request, ...)` builds the resource. Take the profile, fixed code and unit from `ObservationKind` (or the
     canonicals in `Fhir/Common/Telemedicine.cs`), set `subject`/`performer`, and add the `workflow-episodeOfCare`
     extension where the profile requires it. Write dates as FHIR strings (`"yyyy-MM-ddTHH:mm:sszzz"`).
   - `ToModel(resource)` maps the server's response back to the result DTO.

4. **Service call**: `IFhirService` (`Libraries/Hl7Hackathon.Fhir/Services/`) covers metadata, read by id, search, next
   page, create (optionally conditional with `If-None-Exist`) and transaction, all through one private `SendAsync`.
   Add a method there when you need another interaction (e.g. `Patient/[id]/$everything`), with a request-shape test
   in `Tests/Hl7Hackathon.UnitTests/Fhir/FhirServiceTests.cs`. Every method returns a `Result<T>`:
   - a resource that breaks its contract comes back as `Error.Validation` listing each violation by FHIR path, and
     nothing is sent;
   - a server rejection is an `Error` whose message holds the OperationOutcome issues.

5. **Factory**: `Libraries/Hl7Hackathon.Framework/Factories/{Area}Factory.cs` + `Factories/Interfaces/I{Area}Factory.cs`.
   - Validate the request -> `return Error.Validation(...)`.
   - Map to FHIR and call the service; if `result.IsFailure`, `return result.Error;`, otherwise map `result.Value`
     back and return the model.
   - One `catch (Exception)` as a safety net -> log and `return Error.Unexpected(...)`.
   - Register it in `AddDependencyInjection` in `Presentation/Hl7Hackathon.Web/Extensions/WebApplicationBuilderExtensions.cs`.

6. **Persisting locally (optional)**: if the result should also be saved in Postgres, add an entity in
   `Core/Models/Entities/`, a migration in `Core/Migrations/`, and a Dapper service in `Libraries/Hl7Hackathon.Services/`
   (interface in `Services/Interfaces/`), registered in the same `AddDependencyInjection`.

7. **Tests**:
   - Add a builder to `Tests/Hl7Hackathon.TestHelpers/TestFhirResources.cs` for any new resource, returning one that
     meets the IG.
   - Unit: mapper output (profile, codes, extension, references) passes `FhirModelValidator`; new model rules in
     `Fhir/Validation/FhirModelValidatorTests.cs`; one factory test per outcome, with NSubstitute for `IFhirService`.
   - Integration (`Category=FhirServer`, `FhirServerTestsCollection`): post to the HAPI container and read it back.
     HAPI does not know the IG profiles, so this proves the REST flow, not profile validity.

8. **UI**: a MudBlazor page in the web project that injects `I{Area}Factory`, binds to the request DTO and shows
   `result.Error.Message` when `result.IsFailure`. Validation errors arrive as readable text, one issue per line.

9. Run `dotnet build` and `dotnet test --project Tests/Hl7Hackathon.UnitTests`. Then try the flow against the
   selected server and check the request log (`fhir_exchanges`) if the server rejects it.
