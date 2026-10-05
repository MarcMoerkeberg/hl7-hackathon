using System.Net.Http.Headers;
using Dapper;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Hl7Hackathon.Core.Common;
using Hl7Hackathon.Core.Migrations;
using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Services;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.Services;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.Web.Middleware;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace Hl7Hackathon.Web.Extensions;

public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Binds <see cref="AppSettings"/> from the root of the configuration so it can be injected as <see cref="IOptions{TOptions}"/>.
    /// </summary>
    public static AppSettings AddAppSettings(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AppSettings>()
            .Bind(builder.Configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AppSettings appSettings = builder.Configuration.Get<AppSettings>() ?? throw new InvalidOperationException("Failed to load app configuration.");

        return appSettings;
    }

    /// <summary>
    /// Adds Serilog with console and rolling file sinks.
    /// </summary>
    public static void AddSerilog(this WebApplicationBuilder builder)
    {
        var logFilePath = Path.Combine(builder.Environment.ContentRootPath, "logs", "hl7hackathon-.log");

        builder.Services.AddSerilog(
            (services, logger) => logger
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"),
            writeToProviders: true);
    }

    /// <summary>
    /// Adds OpenTelemetry logging, metrics and tracing.
    /// </summary>
    public static void AddTelemetry(this WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))//Use the aspire telemetry
        {
            openTelemetry.UseOtlpExporter();
        }
    }

    /// <summary>
    /// Adds the PostgreSQL data source used by Dapper and the FluentMigrator runner.
    /// </summary>
    public static void AddDatabase(this IServiceCollection services, AppSettings appSettings)
    {
        // Lets Dapper map snake_case columns onto PascalCase properties.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddNpgsqlDataSource(appSettings.DbConnectionString);

        var migrationAssembly = typeof(IMigrationsAssemblyMarker).Assembly;
        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddPostgres()
                .WithGlobalConnectionString(appSettings.DbConnectionString)
                .ScanIn(migrationAssembly)
                .For.Migrations());

        // Untagged migrations always run; tagged ones only when their tag is requested
        string[] migrationTagsToRun = appSettings.SeedDatabase
            ? [DatabaseConstants.SeedTag]
            : Array.Empty<string>();
        services.Configure<RunnerOptions>(options => options.Tags = migrationTagsToRun);
    }

    /// <summary>
    /// Adds <see cref="IFhirService"/> as a typed client for the
    /// configured FHIR server, with resilience and the exchange logging handler.
    /// </summary>
    public static void AddFhirHttpClient(this IServiceCollection services, AppSettings appSettings)
    {
        var timeout = appSettings.FhirServer.RequestTimeout;

        services.AddTransient<FhirExchangeLoggingHandler>();

        // Handlers run in registration order, so every retry attempt is logged separately.
        var fhirHttpClient = services.AddHttpClient<IFhirService, FhirService>(client =>
        {
            client.BaseAddress = new Uri(appSettings.FhirServer.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FhirMediaTypes.Json));
            client.Timeout = Timeout.InfiniteTimeSpan; // The resilience handler owns the timeouts.
        });

        fhirHttpClient.AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.DisableForUnsafeHttpMethods();
            resilience.AttemptTimeout.Timeout = timeout;
            resilience.CircuitBreaker.SamplingDuration = timeout * 2;
            resilience.TotalRequestTimeout.Timeout = timeout * 3;
        });
        fhirHttpClient.AddHttpMessageHandler<FhirExchangeLoggingHandler>();
    }

    /// <summary>
    /// Adds injection for dependencies and their implementations.
    /// </summary>
    public static void AddDependencyInjection(this IServiceCollection services)
    {
        //Factories
        services.AddScoped<IFhirExchangeFactory, FhirExchangeFactory>();
        services.AddScoped<IFhirServerFactory, FhirServerFactory>();
        services.AddScoped<IPatientFactory, PatientFactory>();

        //Services (IFhirService is registered as a typed client in AddFhirHttpClient)
        services.AddScoped<IFhirExchangeService, FhirExchangeService>();
        services.AddScoped<IPatientService, PatientService>();
    }
}
