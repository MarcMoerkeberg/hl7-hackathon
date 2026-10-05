using System.Diagnostics;
using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Hl7Hackathon.Web.Middleware;

/// <summary>Logs every FHIR HTTP exchange and stores it in the fhir_exchanges table.</summary>
public class FhirExchangeLoggingHandler : DelegatingHandler
{
    private readonly string _serverName;
    private readonly IFhirExchangeService _fhirExchangeService;
    private readonly ILogger<FhirExchangeLoggingHandler> _logger;

    public FhirExchangeLoggingHandler(
        IOptions<AppSettings> appSettings,
        IFhirExchangeService fhirExchangeService,
        ILogger<FhirExchangeLoggingHandler> logger)
    {
        _serverName = appSettings.Value.FhirServer.Name;
        _fhirExchangeService = fhirExchangeService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var fhirExchange = new FhirExchange
        {
            ServerName = _serverName,
            Method = request.Method.Method,
            Url = request.RequestUri?.ToString() ?? string.Empty,
            OccurredAt = DateTime.UtcNow
        };

        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
            fhirExchange.RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            fhirExchange.StatusCode = (int)response.StatusCode;
            await response.Content.LoadIntoBufferAsync(cancellationToken);
            fhirExchange.ResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            fhirExchange.Error = ex.Message;
            throw;
        }
        finally
        {
            fhirExchange.DurationMs = (int)stopwatch.ElapsedMilliseconds;
            LogExchange(fhirExchange);
            await StoreExchangeAsync(fhirExchange);
        }
    }

    private void LogExchange(FhirExchange fhirExchange)
    {
        _logger.LogInformation(
            "FHIR {Method} {Url} -> {StatusCode} in {DurationMs} ms ({ServerName})",
            fhirExchange.Method,
            fhirExchange.Url,
            fhirExchange.StatusCode,
            fhirExchange.DurationMs,
            fhirExchange.ServerName);

        _logger.LogDebug(
            "FHIR request body: {RequestBody}{NewLine}FHIR response body: {ResponseBody}",
            fhirExchange.RequestBody,
            Environment.NewLine,
            fhirExchange.ResponseBody);
    }

    // Storing the log must never make the FHIR call itself fail, so this is the one place we swallow errors.
    private async Task StoreExchangeAsync(FhirExchange fhirExchange)
    {
        try
        {
            await _fhirExchangeService.InsertFhirExchangeAsync(fhirExchange, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not store FHIR exchange for {Method} {Url}", fhirExchange.Method, fhirExchange.Url);
        }
    }
}
