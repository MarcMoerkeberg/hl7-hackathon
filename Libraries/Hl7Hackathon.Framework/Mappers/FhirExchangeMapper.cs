using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Models.Entities;

namespace Hl7Hackathon.Framework.Mappers;

internal static class FhirExchangeMapper
{
    public static FhirExchangeSummaryModel ToSummaryModel(FhirExchange fhirExchange) => new()
    {
        Id = fhirExchange.Id,
        ServerName = fhirExchange.ServerName,
        Method = fhirExchange.Method,
        Url = fhirExchange.Url,
        StatusCode = fhirExchange.StatusCode,
        DurationMs = fhirExchange.DurationMs,
        Error = fhirExchange.Error,
        OccurredAtUtc = fhirExchange.OccurredAt
    };

    public static FhirExchangeModel ToModel(FhirExchange fhirExchange) => new()
    {
        Id = fhirExchange.Id,
        ServerName = fhirExchange.ServerName,
        Method = fhirExchange.Method,
        Url = fhirExchange.Url,
        StatusCode = fhirExchange.StatusCode,
        RequestBody = fhirExchange.RequestBody,
        ResponseBody = fhirExchange.ResponseBody,
        DurationMs = fhirExchange.DurationMs,
        Error = fhirExchange.Error,
        OccurredAtUtc = fhirExchange.OccurredAt
    };
}
