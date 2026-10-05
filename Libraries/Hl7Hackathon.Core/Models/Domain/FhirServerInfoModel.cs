namespace Hl7Hackathon.Core.Models.Domain;

public sealed record FhirServerInfoModel
{
    public required string Name { get; init; }

    public required string BaseUrl { get; init; }

    public string? SoftwareName { get; init; }

    public string? SoftwareVersion { get; init; }

    public string? FhirVersion { get; init; }

    public required IReadOnlyList<string> SupportedResourceTypes { get; init; }
}
