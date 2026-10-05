namespace Hl7Hackathon.Core.Models.Domain;

public sealed record FhirExchangeModel
{
    public required long Id { get; init; }

    public required string ServerName { get; init; }

    public required string Method { get; init; }

    public required string Url { get; init; }

    public int? StatusCode { get; init; }

    public string? RequestBody { get; init; }

    public string? ResponseBody { get; init; }

    public required int DurationMs { get; init; }

    public string? Error { get; init; }

    public required DateTime OccurredAtUtc { get; init; }
}
