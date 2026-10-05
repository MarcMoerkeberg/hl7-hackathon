using System.ComponentModel.DataAnnotations;

namespace Hl7Hackathon.Core.Models.Configuration;

public class ServerConnection
{
    /// <summary>The name of the server.</summary>
    /// <remarks>Used for identifying servers in logs and metrics.</remarks>
    [Required]
    public required string Name { get; set; }

    /// <summary>The base url of the server.</summary>
    /// <remarks>Always ends with a trailing slash, whether or not it is configured with one.</remarks>
    [Required]
    [Url]
    public required string BaseUrl
    {
        get;
        set => field = string.IsNullOrEmpty(value) || value.EndsWith('/')
            ? value
            : $"{value}/";
    }

    /// <summary>The request timeout for the server.</summary>
    /// <remarks>Defaults to 60 seconds if nothing is configured.</remarks>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
