using System.Text;
using System.Text.Json;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Mappers;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.CapabilityStatements;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.OperationOutcomes;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.Fhir.Services;

public class FhirService : IFhirService
{
    private readonly HttpClient _httpClient;

    public FhirService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<Result<CapabilityStatement>> GetCapabilityStatementAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "metadata");

        return await SendAsync<CapabilityStatement>(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<TResource>> GetResourceByIdAsync<TResource>(string id, CancellationToken cancellationToken = default)
        where TResource : Resource
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{typeof(TResource).Name}/{Uri.EscapeDataString(id)}");

        return await SendAsync<TResource>(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<Bundle>> SearchResourcesAsync<TResource>(
        IEnumerable<(string Name, string Value)> parameters,
        CancellationToken cancellationToken = default)
        where TResource : Resource
    {
        var query = string.Join("&", parameters.Select(parameter => $"{parameter.Name}={Uri.EscapeDataString(parameter.Value)}"));
        var resourceType = typeof(TResource).Name;

        using var request = new HttpRequestMessage(HttpMethod.Get, query.Length == 0 ? resourceType : $"{resourceType}?{query}");

        return await SendAsync<Bundle>(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<Bundle?>> GetNextPageAsync(Bundle currentPage, CancellationToken cancellationToken = default)
    {
        var nextUrl = currentPage.Link
            ?.FirstOrDefault(link => string.Equals(link.Relation, "next", StringComparison.OrdinalIgnoreCase))
            ?.Url;
        if (nextUrl is null)
        {
            return Result<Bundle?>.Success(null);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
        var nextPage = await SendAsync<Bundle>(request, cancellationToken);

        return nextPage.IsSuccess
            ? nextPage.Value
            : nextPage.Error;
    }

    /// <inheritdoc/>
    public async Task<Result<TResource>> CreateResourceAsync<TResource>(
        TResource resource,
        string? ifNoneExist = null,
        CancellationToken cancellationToken = default)
        where TResource : Resource
    {
        if (ValidateContract(resource) is { } contractError)
        {
            return contractError;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, resource.GetType().Name)
        {
            Content = new StringContent(FhirJsonSerializer.Serialize(resource), Encoding.UTF8, FhirMediaTypes.Json)
        };

        // Return the representation of the created resource, without it the server answers 201.
        request.Headers.Add("Prefer", "return=representation");
        if (ifNoneExist is not null)
        {
            request.Headers.Add("If-None-Exist", ifNoneExist);
        }

        return await SendAsync<TResource>(request, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<Bundle>> PostTransactionAsync(Bundle transaction, CancellationToken cancellationToken = default)
    {
        if (ValidateContract(transaction) is { } contractError)
        {
            return contractError;
        }

        // An empty relative URL posts to the base URL itself, which is where transactions go.
        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = new StringContent(FhirJsonSerializer.Serialize(transaction), Encoding.UTF8, FhirMediaTypes.Json)
        };

        // Return the representation of the created resource, without it the server answers 201.
        request.Headers.Add("Prefer", "return=representation");

        return await SendAsync<Bundle>(request, cancellationToken);
    }

    /// <summary>
    /// Validates the <paramref name="resource"/> against the contract of its models before it is sent.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <returns>A validation <see cref="Error"/> listing every violation, or <see langword="null"/> when the resource is valid.</returns>
    private static Error? ValidateContract(Resource resource)
    {
        var violations = FhirModelValidator.Validate(resource);

        return violations.Count == 0
            ? null
            : Error.Validation($"{resource.GetType().Name} does not meet its FHIR contract:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// Sends a HTTP request and deserializes the response into a FHIR resource of type <typeparamref name="TResource"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the FHIR resource.</typeparam>
    /// <param name="request">The HTTP request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A new instance of <see cref="Result{TResource}"/> representing the outcome.</returns>
    private async Task<Result<TResource>> SendAsync<TResource>(HttpRequestMessage request, CancellationToken cancellationToken)
        where TResource : Resource
    {
        var requestDescription = $"{request.Method} {request.RequestUri}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // No response at all (DNS, refused connection, TLS); timeouts from the resilience handler still throw.
            return Error.Unavailable($"{requestDescription} could not reach the FHIR server: {ex.Message}");
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var outcome = TryDeserializeOperationOutcome(json);
                return FhirServerError.FromResponse(requestDescription, response, outcome);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return Error.Unexpected($"{requestDescription} returned an empty body.");
            }

            return DeserializeResponse<TResource>(requestDescription, json);
        }
    }

    /// <summary>
    /// Deserializes the JSON response into a FHIR resource of type <typeparamref name="TResource"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the FHIR resource.</typeparam>
    /// <param name="requestDescription">The method and URI of the HTTP request.</param>
    /// <param name="json">The JSON response to deserialize.</param>
    /// <returns>A new instance of <see cref="Result{TResource}"/> representing the outcome of the deserialization.</returns>
    private static Result<TResource> DeserializeResponse<TResource>(string requestDescription, string json)
        where TResource : Resource
    {
        try
        {
            return FhirJsonSerializer.Deserialize(json) is TResource resource
                ? resource
                : Error.Unexpected($"{requestDescription} did not return a {typeof(TResource).Name}.");
        }
        catch (JsonException ex)
        {
            return Error.Unexpected($"{requestDescription} returned JSON that does not fit the {typeof(TResource).Name} model: {ex.Message}");
        }
    }

    private static OperationOutcome TryDeserializeOperationOutcome(string json)
    {
        try
        {
            return FhirJsonSerializer.Deserialize<OperationOutcome>(json);
        }
        catch (JsonException ex)
        {
            return new OperationOutcome
            {
                Issue = new List<OperationOutcomeIssue>
                {
                    new OperationOutcomeIssue
                    {
                        Severity = "error",
                        Code = "processing",
                        Diagnostics = $"Failed to deserialize OperationOutcome from JSON: {json}",
                        Details = new CodeableConcept { Text = ex.Message}
                    }
                }
            };
        }
    }
}
