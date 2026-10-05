# CLAUDE.md

Preparation project for the HL7 DK hackathon 2026, telemedicine track: a .NET 10 Blazor Server + MudBlazor app that
creates and searches FHIR R4 resources on a test FHIR server. See `README.md` for running it.

## Layout and layering

```
Presentation/Hl7Hackathon.AppHost   Aspire host: Postgres, pgAdmin, optional local HAPI FHIR, starts the web app
Presentation/Hl7Hackathon.Web       Blazor Server + MudBlazor; composition root (Extensions/), options validator (Configuration/),
                                    FHIR exchange logging handler (Middleware/)
Libraries/Hl7Hackathon.Core         Models/: Configuration (AppSettings), Entities, Domain (DTOs); Results/ (Result, Error); Migrations/
Libraries/Hl7Hackathon.Services     Database access only (Dapper + NpgsqlDataSource)
Libraries/Hl7Hackathon.Fhir         FHIR server access: Services/ (typed HttpClient), Models/ (one folder per resource with
                                    2+ types, DataTypes/), Codes/ (smart enums), Validation/, Mappers/ (FHIR error mapping),
                                    Common/ (JSON, media type, IG canonicals)
Libraries/Hl7Hackathon.Framework    Factories/ (business logic), Mappers/ (entity/FHIR -> DTO)
Tests/                              UnitTests (NSubstitute), IntegrationTests (Testcontainers), TestHelpers (fixtures)
```

Dependencies point inward: Presentation -> Framework -> {Fhir, Services} -> Core; Fhir and Services don't know each
other. Interfaces live in an `Interfaces/` subfolder next to their implementations (`Factories/Interfaces/`,
`Services/Interfaces/`, `Fhir/Services/Interfaces/`). Namespaces match folders.

- **Configuration**: `Core/Models/Configuration/AppSettings.cs` mirrors the root of `appsettings.json`. It is bound
  with `IOptions<AppSettings>` in `AddAppSettings` and validated at startup from its data annotations
  (`ValidateDataAnnotations().ValidateOnStart()`). Inject `IOptions<AppSettings>` wherever settings are needed.
  `ServerConnection.BaseUrl` always ends with `/`, whether or not it is configured with one.
- **Composition root**: `Web/Program.cs` calls the extensions in `Web/Extensions/WebApplicationBuilderExtensions.cs`
  (`AddAppSettings`, `AddSerilog`, `AddTelemetry`, `AddDatabase`, `AddFhirHttpClient`, `AddDependencyInjection`) and
  `WebApplicationExtensions.RunMigrations()`. Register new factories and services in `AddDependencyInjection`.
  `Tests/Hl7Hackathon.TestHelpers/TestAppFactory.cs` repeats the backend calls, so keep the two in step.
  The AppHost has its own `Extensions/DistributedApplicationBuilderExtensions.cs`.
- **Results, not exceptions, for expected failures**: `Core/Results/` has `Result`, `Result<T>` and `Error`
  (`ErrorType` + message). Return a value or an `Error` directly; the implicit conversions wrap them. Services return
  `Result<T>` for every outcome they understand. Factories return `Result<T>`, pass a failed service result on with
  `return result.Error;`, and keep one `catch (Exception)` only as a safety net that logs and returns
  `Error.Unexpected`. Factories are the only thing Blazor components call.
- **FHIR without an SDK**: there is no Firely or other FHIR library; learning the wire format is the point.
  `Fhir/Services/FhirService.cs` builds every request itself on its injected `HttpClient`. It is a typed client,
  registered in `AddFhirHttpClient` (not `AddDependencyInjection`, which would override it) with the base URL, the
  `application/fhir+json` Accept header, resilience and the logging handler. Resources are our own records in
  `Fhir/Models/`, named after the spec and holding only the elements we use; add elements and resources there as tasks
  need them. `Fhir/Common/FhirJsonSerializer.cs` turns them into FHIR JSON with System.Text.Json (`resourceType` is the
  polymorphic discriminator on `Resource`).
- **FHIR models document and enforce their contract.** Each type links its IG profile (or the R4 spec) and each property
  carries the element's short description. The C# `required` keyword follows base R4 only, so reading other people's
  resources stays tolerant. IG constraints are data annotations (`[Required]`, `[MinLength(1)]`, `[FhirDate]`,
  `[FhirDateTime]`) plus `IValidatableObject` for profile rules (meta.profile, fixed codes and units, the
  workflow-episodeOfCare extension). `FhirService` runs `Fhir/Validation/FhirModelValidator.cs` over the whole tree
  before every create or transaction and returns `Error.Validation` listing every violation.
- **Codes are smart enums** in `Fhir/Codes/`: a required value set on a model we send is a `FhirCode<T>` record
  (`ObservationStatus.Final`), written as the bare code by `FhirCodeJsonConverterFactory`; an unknown code fails
  deserialization with a `JsonException`. Receive-only models keep plain strings. `ObservationKind` groups the profile,
  fixed code and unit of each IG Observation; IG canonicals live in `Fhir/Common/Telemedicine.cs`.
- **Factory signatures use DTOs from `Core/Models` only.** FHIR models (`Fhir.Models`) stay in Fhir and Framework.
- **Services** do data access only. `FhirService` turns a non-success response into an `Error` via
  `Fhir/Mappers/FhirServerError.cs` (HTTP status -> `ErrorType`, OperationOutcome issues -> message), and catches
  only what is an expected outcome of the call: `HttpRequestException` (unreachable) and `JsonException` (body does
  not fit our model). Database services return the entity or null and let failures throw.
- Every FHIR HTTP call goes through `Web/Middleware/FhirExchangeLoggingHandler.cs`, which logs it and stores it in the
  `fhir_exchanges` table.
- The entity `Core.Models.Entities.Patient` shares its name with `Fhir.Models.Patient`; alias one of them
  (`using FhirPatient = Hl7Hackathon.Fhir.Models.Patient;`) in files that need both.

For detailed code conventions load the `code-guidelines` skill; for the FHIR profiles and codes load
`fhir-telemedicine-ig`; to add a new FHIR use case end to end, load `add-fhir-feature`.

## Commands

```bash
dotnet build
dotnet test --project Tests/Hl7Hackathon.UnitTests
dotnet test --project Tests/Hl7Hackathon.IntegrationTests --filter-not-trait "Category=FhirServer"
dotnet run --project Presentation/Hl7Hackathon.AppHost
```

- Tests run on Microsoft.Testing.Platform (`global.json`); use `--project`, `--filter-trait` / `--filter-not-trait`.
- Integration tests and the AppHost need Docker running.
- Unit test folders mirror the source projects; build FHIR test data from `TestHelpers/TestFhirResources.cs`.
  `ArchitectureTests` guards the project dependency rules.
- Package versions live in `Directory.Packages.props` (central package management); never put versions in a csproj.
- `nuget.config` pins nuget.org as the only source.

## Conventions that are easy to miss

- PostgreSQL naming is snake_case. Dapper maps it to PascalCase via `DefaultTypeMap.MatchNamesWithUnderscores`.
- SQL is a `const string` raw string literal, one clause per line, always parameterized.
- Timestamps are stored as `timestamptz` and written as UTC `DateTime` (`DateTime.UtcNow`).
- Migrations are `Core/Migrations/{Description}.cs` with `[Migration(YYYYMMDDNNNN)]`. Seed data migrations carry
  `[Tags(DatabaseConstants.SeedTag)]` and only run when `SeedDatabase` is true.
- The repo is developed on Windows and run on Linux: `.gitattributes` forces LF, use `Path.Combine`, and remember
  that file names are case-sensitive on Linux.
- Only fictitious patient data. The public HAPI server is visible to everyone.
- Comments explain *why*, at most two lines. No commented-out code.
