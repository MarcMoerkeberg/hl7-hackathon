using System.ComponentModel.DataAnnotations;
using Hl7Hackathon.Fhir.Codes;

namespace Hl7Hackathon.Fhir.Models.Bundles;

/// <summary>
/// A collection of resources: the results of a search, or a transaction to send.
/// </summary>
/// <remarks>Contract: <see href="https://hl7.org/fhir/R4/bundle.html">Bundle (R4)</see>.</remarks>
public sealed record Bundle : Resource, IValidatableObject
{
    /// <summary>document | message | transaction | transaction-response | batch | batch-response | history | searchset | collection</summary>
    public required BundleType Type { get; init; }

    /// <summary>If search, the total number of matches.</summary>
    /// <remarks>Counts the matches across all pages, not only this one.</remarks>
    public int? Total { get; init; }

    /// <summary>Links related to this Bundle.</summary>
    [MinLength(1)]
    public IReadOnlyList<BundleLink>? Link { get; init; }

    /// <summary>Entry in the bundle - will have a resource or information.</summary>
    [MinLength(1)]
    public IReadOnlyList<BundleEntry>? Entry { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Invariant bdl-3.
        if ((Type == BundleType.Transaction || Type == BundleType.Batch) && Entry?.Any(entry => entry.Request is null) == true)
        {
            yield return new ValidationResult($"Every entry.request is mandatory for a {Type}.", [nameof(Entry)]);
        }
    }
}
