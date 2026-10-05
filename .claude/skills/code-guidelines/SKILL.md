---
name: code-guidelines
description: Code conventions for this repo - layering (Core/Services/Fhir/Framework/Presentation), Result/Error, Dapper SQL style, hand-written FHIR models with smart enums and contract validation, naming, DI extensions, comments and tests. Use when writing, reviewing or refactoring any C# in this repository.
---

# Code guidelines

## Layers

| Layer | Project | Does | Never does |
|---|---|---|---|
| Presentation | `Presentation/*` | UI, routing, calling factories, showing `Result` errors; composition root and the FHIR exchange logging handler | Business logic, data access |
| Framework | `Libraries/Hl7Hackathon.Framework` | Validation, orchestration, mapping, exception boundary | SQL, raw HTTP |
| Fhir | `Libraries/Hl7Hackathon.Fhir` | FHIR REST through the typed `HttpClient`, FHIR models, smart enum codes, contract validation | Database access, business rules |
| Services | `Libraries/Hl7Hackathon.Services` | SQL through Dapper | FHIR, business rules, catching exceptions |
| Core | `Libraries/Hl7Hackathon.Core` | `AppSettings`, DTOs, entities, `Result`/`Error`, migrations | Referencing other layers |

Framework -> {Fhir, Services} -> Core. Fhir and Services never reference each other; `ArchitectureTests` enforces it.

A factory that only forwards to one service with no validation, mapping or error handling is a smell. Either the logic
is missing, or the factory isn't needed.

## Factories

- Return `Result` or `Result<T>` from `Core/Results/`. Never throw and never return a bare model.
- Expected failures are values, not exceptions: validate input first and `return Error.Validation(...)`,
  `return Error.NotFound(...)` for missing data, and pass a failed service result on with `return result.Error;`
  (log it as a warning first if it is worth seeing). Read `result.Value` only after checking `IsSuccess`.
- Wrap the body in one `try`/`catch (Exception)` as a safety net for bugs and infrastructure failures (database down,
  resilience timeouts): log with `LogError` and return `Error.Unexpected(...)`. No type-specific catches.
- Return the value or the `Error` directly and let the implicit conversions build the result. A concrete list
  (`List<T>` from `ToList()`) converts to `Result<IReadOnlyList<T>>`; a variable typed as the interface does not,
  because C# ignores user-defined conversions from interfaces, so use `Result<IReadOnlyList<T>>.Success(list)` there.
- Parameters and return types are DTOs from `Core/Models/Domain/` only, never FHIR models or entities.
- Implementations live in `Factories/`, interfaces in `Factories/Interfaces/` (same for `Services/Interfaces/` and
  `Fhir/Services/Interfaces/`). Namespaces match the folders.

## Database services

- Inject `NpgsqlDataSource` and use Dapper directly. There is no repository wrapper.
- SQL is a `const string` raw string literal (`"""`), one clause per line, parameterized, with snake_case columns listed
  explicitly (no `SELECT *`).
- Pass the `CancellationToken` with `new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)`.
- Return the entity or null and let failures throw; the factory's safety net handles them.

```csharp
public async Task<Patient?> GetPatientByIdAsync(long patientId, CancellationToken cancellationToken = default)
{
    const string sql = """
        SELECT
            id,
            cpr,
            given_name
        FROM patients
        WHERE id = @PatientId
        """;

    await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
    var command = new CommandDefinition(sql, new { PatientId = patientId }, cancellationToken: cancellationToken);

    return await connection.QueryFirstOrDefaultAsync<Patient>(command);
}
```

## FHIR

- `FhirService` is a typed client: it takes `HttpClient` in the constructor and is registered with
  `AddHttpClient<IFhirService, FhirService>(...)` in `AddFhirHttpClient`. Each method builds the `HttpRequestMessage`
  explicitly (relative URL, headers, a `StringContent` from `FhirJsonSerializer.Serialize`), reads the body as a string
  and returns `Result<T>`: `FhirServerError.FromResponse` on a non-success status, otherwise the deserialized resource.
  It catches only `HttpRequestException` and `JsonException`, the expected failures of the call itself.
- Before a create or transaction it runs `FhirModelValidator` and returns `Error.Validation` without sending anything
  when the resource breaks its contract.
- Models are `sealed record`s named after the spec, in `Fhir/Models/`. A resource with two or more types gets its own
  plural folder (`Bundles/`, `QuestionnaireResponses/`), datatypes live in `DataTypes/`; singles stay in the root.
  Backbone elements are named `{Resource}{Element}` (`BundleEntry`), because C# forbids a nested type and a property
  with the same name, which FHIR uses everywhere (`rest.resource`).
- JSON names come from the camelCase policy, so property names must match the spec exactly; choice elements get one
  property per type used (`EffectivePeriod`, `ValueQuantity`). A new resource also needs a `[JsonDerivedType]` on `Resource`.
- Each type's `<remarks>` links its contract: the IG profile page, or the R4 page when there is no profile. Property
  summaries are the element's short description from the IG/spec.
- `required` follows base R4 only (1..1 in R4), so reading other people's resources stays tolerant. Lists are
  `IReadOnlyList<T>?` and stay null rather than empty.
- IG constraints on models we send are data annotations: `[Required]`, `[MinLength(1)]` on every list (FHIR forbids
  empty arrays), `[Length(1, 1)]`, `[FhirDate]`, `[FhirDateTime]`. Rules attributes can't express (meta.profile, fixed
  codes and units, extensions, R4 invariants such as qty-3) go in `IValidatableObject.Validate`. Receive-only models
  (CapabilityStatement, OperationOutcome, Questionnaire) get docs but no annotations.
- A `code` from a required value set on a model we send is a smart enum in `Fhir/Codes/`: a `sealed record` deriving
  from `FhirCode<TSelf>` with a private constructor and one `public static readonly` field per code, documented with
  the value set's definition. The JSON converter writes the bare code and rejects unknown codes. Receive-only models
  keep plain strings.
- IG canonicals live in `Fhir/Common/Telemedicine.cs`; what each kind of Observation fixes (profile, code, unit) lives
  in `ObservationKind`.

## Core

- DTOs are `sealed record`s with `required` init properties, suffixed `Model`, in `Models/Domain/`.
- Entities (`Models/Entities/`) are plain classes with get/set properties that mirror the table.
- Configuration models (`Models/Configuration/`) mirror `appsettings.json`; `AppSettings` is the root. Validate with data
  annotations (`[Required]`, `[Url]`, `[Range]`) and `[ValidateObjectMembers]` on nested objects; `AddAppSettings`
  binds them with `ValidateDataAnnotations().ValidateOnStart()`. Consumers inject `IOptions<AppSettings>`.
- Migrations: `Migrations/{Description}.cs` (class named after the file) with `[Migration(YYYYMMDDNNNN)]` and a working `Down()`.
  Timestamps use `AsDateTimeOffset()` (timestamptz). Seed migrations get `[Tags(DatabaseConstants.SeedTag)]`.

## Dependency injection

`Program.cs` calls the extensions in `Presentation/Hl7Hackathon.Web/Extensions/WebApplicationBuilderExtensions.cs`
(`AddAppSettings`, `AddSerilog`, `AddTelemetry`, `AddDatabase`, `AddFhirHttpClient`, `AddDependencyInjection`). Add new
factories and database services to `AddDependencyInjection` under the `//Factories` and `//Services` comments; both are
scoped. FHIR services are typed clients and go in `AddFhirHttpClient` instead, which also wires resilience and
`Web/Middleware/FhirExchangeLoggingHandler`. `Tests/Hl7Hackathon.TestHelpers/TestAppFactory.cs` repeats the backend
calls, so keep the two in step. The AppHost's container setup lives in `Presentation/Hl7Hackathon.AppHost/Extensions/`.

## Naming

- PascalCase for types, methods, properties and constants; camelCase for locals and parameters; `_camelCase` for private fields.
- Async methods end with `Async` and take `CancellationToken cancellationToken = default` last.
- Name methods after what they return (`GetRecentFhirExchangesAsync`, not `Get`).
- One public type per file, named after the type.
- Folders are only created once there are at least two files that belong in them.

## Comments

- At most two lines, explaining *why* (a trap, an external contract, a non-obvious invariant). Never what the code does.
- No commented-out code, no history notes, no banners.
- XML docs only where they add information beyond the signature.

## Tests

- xUnit v3 on Microsoft.Testing.Platform, NSubstitute for mocks, Testcontainers for Postgres and HAPI FHIR.
- Unit test folders mirror the source: `Architecture/`, `Configuration/`, `Core/`, `Factories/`, `Fhir/`, `Web/`;
  `Stubs/StubHttpMessageHandler` records requests and answers with queued responses or exceptions.
- Build FHIR test data from `TestHelpers/TestFhirResources`, which returns resources that meet the IG; break one
  element with `with` to test a rule.
- Mark each class with `[Trait("Category", "Unit")]`, `"Integration"` or `"FhirServer"` (tests that need the HAPI container).
- Integration test classes use `[Collection(IntegrationTestsCollection.Name)]` or `[Collection(FhirServerTestsCollection.Name)]`.
- Use Arrange / Act / Assert comments. Name tests `Method_ReturnsX_WhenY`.
- Every factory method gets a test for each outcome it can return: the value, and each `ErrorType` (`Validation`,
  `NotFound`, a passed-on service error, `Unexpected` from the safety net).
- Assert on `result.Error?.Type`, not on the message text, unless the message is the point of the test.
- Pass `TestContext.Current.CancellationToken` into async calls.
