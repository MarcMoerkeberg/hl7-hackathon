using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.CapabilityStatements;

namespace Hl7Hackathon.Fhir.Services.Interfaces;

/// <summary>
/// A service for interaction with a FHIR server.
/// </summary>
/// <remarks>For more information about FHIR actions see <see href="https://hl7.org/fhir/">the documentation</see>.</remarks>
public interface IFhirService
{
    /// <summary>
    /// Gets the FHIR server's capability statement.
    /// </summary>
    /// <remarks>For more information about capability statements see <see href="https://hl7.org/fhir/R4/capabilitystatement.html">the documentation</see>.</remarks>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the <see cref="CapabilityStatement"/> if successful.
    /// </returns>
    Task<Result<CapabilityStatement>> GetCapabilityStatementAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the <typeparamref name="TResource"/> from the <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource to retrieve.</typeparam>
    /// <param name="id">The id of the resource to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the <typeparamref name="TResource"/> if successful.
    /// </returns>
    Task<Result<TResource>> GetResourceByIdAsync<TResource>(string id, CancellationToken cancellationToken = default)
        where TResource : Resource;

    /// <summary>
    /// Searches for <typeparamref name="TResource"/> resources matching the specified <paramref name="parameters"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource to search for.</typeparam>
    /// <param name="parameters">The search parameters as name-value pairs.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing a <see cref="Bundle"/> of <typeparamref name="TResource"/> if successful.
    /// </returns>
    Task<Result<Bundle>> SearchResourcesAsync<TResource>(
        IEnumerable<(string Name, string Value)> parameters,
        CancellationToken cancellationToken = default)
        where TResource : Resource;

    /// <summary>
    /// Gets the next page of a paginated search result using the <paramref name="currentPage"/>.
    /// </summary>
    /// <param name="currentPage">The current page of the search result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the next page <see cref="Bundle"/> if successful, or <see langword="null"/> if there are no more pages.
    /// </returns>
    Task<Result<Bundle?>> GetNextPageAsync(Bundle currentPage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <typeparamref name="TResource"/> on the FHIR server.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource to create.</typeparam>
    /// <param name="resource">The resource to create.</param>
    /// <param name="ifNoneExist">An optional conditional create parameter (e.g. "identifier=system|value").</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the created <typeparamref name="TResource"/> if successful.
    /// </returns>
    Task<Result<TResource>> CreateResourceAsync<TResource>(
        TResource resource,
        string? ifNoneExist = null,//TODO: Change to a typed record
        CancellationToken cancellationToken = default)
        where TResource : Resource;

    /// <summary>
    /// Posts a FHIR transaction bundle to the server.
    /// </summary>
    /// <param name="transaction">The transaction bundle to post.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> representing the outcome and containing the resulting <see cref="Bundle"/> if successful.
    /// </returns>
    Task<Result<Bundle>> PostTransactionAsync(Bundle transaction, CancellationToken cancellationToken = default);
}
