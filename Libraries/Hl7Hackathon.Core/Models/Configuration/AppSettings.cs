using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Hl7Hackathon.Core.Models.Configuration;

public record AppSettings
{
    /// <summary>The FHIR server to send requests to.</summary>
    [Required]
    [ValidateObjectMembers]
    public required ServerConnection FhirServer { get; init; }

    /// <summary>The connection string to the database.</summary>
    /// <remarks>Is expected to be a PostgreSQL server.</remarks>
    [Required]
    public required string DbConnectionString { get; init; }

    /// <summary>Whether the seed migrations should run.</summary>
    /// <remarks>Defaults to false if nothing is configured.</remarks>
    public bool SeedDatabase { get; init; } = false;
}
