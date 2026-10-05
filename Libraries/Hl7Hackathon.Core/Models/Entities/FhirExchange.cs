namespace Hl7Hackathon.Core.Models.Entities;

public class FhirExchange
{
    public long Id { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public int? StatusCode { get; set; }

    public string? RequestBody { get; set; }

    public string? ResponseBody { get; set; }

    public int DurationMs { get; set; }

    public string? Error { get; set; }

    public DateTime OccurredAt { get; set; }
}
