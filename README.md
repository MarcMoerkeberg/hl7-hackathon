# HL7 DK Hackathon 2026 – Telemedicine track

Preparation project for the [HL7 Denmark hackathon](https://hl7.dk/fhir/hackathons/) (6 October 2026, MedCom, Odense),
track 2 [Telemedicine](https://hl7.dk/fhir/hackathons/track-telemedicine.html): send and search FHIR R4 resources
for a mental-health telemonitoring case (Patient, EpisodeOfCare, sleep/activity Observations, QuestionnaireResponse).

## Stack

- .NET 10, C#
- FHIR REST over a typed `HttpClient`, with hand-written resource records and System.Text.Json instead of a FHIR SDK;
  the records check the IG profile rules before anything is sent
- PostgreSQL with Dapper and FluentMigrator
- Aspire AppHost that starts PostgreSQL (and optionally a local HAPI FHIR server) in Docker
- Serilog (console + rolling file) and OpenTelemetry (Aspire dashboard)
- xUnit v3, NSubstitute and Testcontainers for tests
- Blazor Server + MudBlazor frontend

## Prerequisites

- .NET 10 SDK (`global.json` accepts any 10.0 feature band at or above 10.0.100)
- Docker. On Linux, make sure your user can run `docker` without `sudo` (be in the `docker` group);
  Aspire and Testcontainers both need that.

## Solution layout

```
Presentation/
  Hl7Hackathon.AppHost          Aspire host: starts Postgres, pgAdmin, optional HAPI FHIR, and the web app
  Hl7Hackathon.Web              Blazor Server + MudBlazor; registers everything (Extensions/), FHIR exchange log (Middleware/)
Libraries/
  Hl7Hackathon.Core             AppSettings, DTO models, entities, Result/Error, migrations
  Hl7Hackathon.Services         Postgres access (Dapper)
  Hl7Hackathon.Fhir             FHIR server access: typed HttpClient, FHIR models, smart enum codes, contract validation
  Hl7Hackathon.Framework        Factories (business logic), entity/FHIR -> DTO mapping
Tests/
  Hl7Hackathon.UnitTests        Factories, FHIR client/JSON/validation, configuration, architecture rules
  Hl7Hackathon.IntegrationTests Postgres and HAPI FHIR in Testcontainers
  Hl7Hackathon.TestHelpers      Shared fixtures and IG-valid test resources
```

Framework depends on Fhir and Services, which both depend only on Core; Fhir and Services never reference each other.

## Running

```bash
dotnet run --project Presentation/Hl7Hackathon.AppHost
```

This opens the Aspire dashboard (http://localhost:15180), starts PostgreSQL and pgAdmin in Docker, waits for the
database and then starts the web app (its URL is listed in the dashboard). The containers are persistent with a
data volume, so data survives restarts.

- Local HAPI FHIR server: set `LocalFhirServer:Enabled` to `true` in `Presentation/Hl7Hackathon.AppHost/appsettings.json`.
  The AppHost then starts HAPI on http://localhost:8080/fhir and points the web app at it instead of its configured
  `FhirServer`. The first pull is large and startup takes about a minute.
- Without Aspire: `docker compose up -d` (add `--profile fhir` for HAPI), then
  `dotnet run --project Presentation/Hl7Hackathon.Web`. `appsettings.Development.json` already holds the matching
  connection string.
- Reset the database: stop the AppHost, then remove the `postgres` container and its volume in Docker.

### What the AppHost does

The AppHost is an [Aspire](https://aspire.dev) project used for local development only; it is never deployed.
`Presentation/Hl7Hackathon.AppHost/Extensions/DistributedApplicationBuilderExtensions.cs` describes the pieces the
app needs, and `dotnet run` on the AppHost then:

- starts PostgreSQL and pgAdmin (and HAPI FHIR when enabled) as Docker containers, kept between runs;
- starts the web app with the database's connection string injected as `DbConnectionString`, and waits until the
  database is healthy first;
- shows everything in the dashboard: each resource's state and URL, console logs, and the web app's OpenTelemetry
  logs, traces (including every FHIR HTTP call) and metrics.

`docker-compose.yml` is the same setup without Aspire.

## Configuration

`appsettings.json` of the web app is bound to `AppSettings` (`Libraries/Hl7Hackathon.Core/Models/Configuration/`)
and validated at startup; the app refuses to start with a message naming the invalid key.

| Key | Meaning |
|---|---|
| `DbConnectionString` | PostgreSQL connection string (set by the AppHost; `appsettings.Development.json` has the docker compose one) |
| `SeedDatabase` | `true` runs the seed migration with fictitious patients |
| `FhirServer:Name` | Name used in logs and the exchange log |
| `FhirServer:BaseUrl` | FHIR base URL, with or without a trailing slash |
| `FhirServer:RequestTimeout` | Per-attempt timeout, e.g. `00:01:00` (default 60 seconds, max 10 minutes) |

To use the hackathon server on the day, change `FhirServer` in `Presentation/Hl7Hackathon.Web/appsettings.json`
(or set `FhirServer__BaseUrl` as an environment variable) and restart.

The public HAPI server is shared with the whole world: only send fictitious data.

## Tests

Tests run on Microsoft.Testing.Platform (enabled in `global.json`).

```bash
dotnet test --project Tests/Hl7Hackathon.UnitTests
dotnet test --project Tests/Hl7Hackathon.IntegrationTests --filter-not-trait "Category=FhirServer"
dotnet test --project Tests/Hl7Hackathon.IntegrationTests
```

Integration tests need Docker. `Category=FhirServer` tests start a HAPI FHIR container, which is slow the first time.

- Unit test folders mirror the source projects. `Architecture/ArchitectureTests` fails when a project references a
  layer it must not (for example Fhir and Services referencing each other).
- `Tests/Hl7Hackathon.TestHelpers/TestFhirResources.cs` builds resources that meet the IG; tests break one element
  with `with` to check a rule.

## Useful links

- Hackathon IG: https://hl7.dk/fhir/hackathons/ (artifacts: https://hl7.dk/fhir/hackathons/artifacts.html)
- IG source (FSH): https://github.com/hl7dk/dk-hackathons/tree/main/input/fsh
- FHIR R4: https://hl7.org/fhir/R4/
- DK Core: https://hl7.dk/fhir/core/
- FUT IG (the national telemedicine IG this case is modelled on): https://docs.ehealth.sundhed.dk/latest-released/ig/
- Zulip #nordics: https://chat.fhir.org/#narrow/channel/194447-nordics
