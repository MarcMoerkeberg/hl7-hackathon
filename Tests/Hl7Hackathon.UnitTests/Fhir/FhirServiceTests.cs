using System.Net;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Services;
using Hl7Hackathon.TestHelpers;
using Hl7Hackathon.UnitTests.Stubs;

namespace Hl7Hackathon.UnitTests.Fhir;

[Trait("Category", "Unit")]
public class FhirServiceTests
{
    private const string BaseUrl = "https://fhir.example.org/baseR4/";

    private readonly StubHttpMessageHandler _handler = new();
    private readonly FhirService _service;

    public FhirServiceTests()
    {
        _service = new FhirService(new HttpClient(_handler) { BaseAddress = new Uri(BaseUrl) });
    }

    [Fact]
    public async Task GetCapabilityStatementAsync_GetsMetadata()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "CapabilityStatement", "status": "active", "fhirVersion": "4.0.1" }""");

        // Act
        var result = await _service.GetCapabilityStatementAsync(TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"{BaseUrl}metadata", request.Uri.ToString());
        Assert.Equal("4.0.1", result.Value.FhirVersion);
    }

    [Fact]
    public async Task GetResourceByIdAsync_GetsTheTypeAndId()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Patient", "id": "123" }""");

        // Act
        var result = await _service.GetResourceByIdAsync<Patient>("123", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal($"{BaseUrl}Patient/123", Assert.Single(_handler.Requests).Uri.ToString());
        Assert.Equal("123", result.Value.Id);
    }

    [Fact]
    public async Task GetResourceByIdAsync_ReturnsUnexpectedError_WhenTheServerReturnsAnotherResourceType()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Bundle", "type": "searchset" }""");

        // Act
        var result = await _service.GetResourceByIdAsync<Patient>("123", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
        Assert.Contains("did not return a Patient", result.Error?.Message);
    }

    [Fact]
    public async Task SearchResourcesAsync_EncodesValues_AndKeepsRepeatedNames()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Bundle", "type": "searchset", "total": 0 }""");

        // Act
        await _service.SearchResourcesAsync<Observation>(
            [
                ("code", "http://snomed.info/sct|248263006"),
                ("date", "ge2026-10-01"),
                ("date", "le2026-10-06")
            ],
            TestContext.Current.CancellationToken);

        // Assert
        var uri = Assert.Single(_handler.Requests).Uri;
        Assert.Equal($"{BaseUrl}Observation", uri.GetLeftPart(UriPartial.Path));
        Assert.Equal("?code=http%3A%2F%2Fsnomed.info%2Fsct%7C248263006&date=ge2026-10-01&date=le2026-10-06", uri.Query);
    }

    [Fact]
    public async Task SearchResourcesAsync_GetsTheWholeType_WhenThereAreNoParameters()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Bundle", "type": "searchset" }""");

        // Act
        await _service.SearchResourcesAsync<Patient>([], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal($"{BaseUrl}Patient", Assert.Single(_handler.Requests).Uri.ToString());
    }

    [Fact]
    public async Task GetNextPageAsync_FollowsTheNextLink()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Bundle", "type": "searchset" }""");
        var nextUrl = $"{BaseUrl}?_getpages=abc&_getpagesoffset=20";
        var currentPage = new Bundle
        {
            Type = BundleType.Searchset,
            Link = [new BundleLink { Relation = "self", Url = BaseUrl }, new BundleLink { Relation = "next", Url = nextUrl }]
        };

        // Act
        var result = await _service.GetNextPageAsync(currentPage, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Equal(nextUrl, Assert.Single(_handler.Requests).Uri.ToString());
    }

    [Fact]
    public async Task GetNextPageAsync_ReturnsNull_OnTheLastPage()
    {
        // Arrange
        var lastPage = new Bundle { Type = BundleType.Searchset, Link = [new BundleLink { Relation = "self", Url = BaseUrl }] };

        // Act
        var result = await _service.GetNextPageAsync(lastPage, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task CreateResourceAsync_PostsFhirJsonToTheResourceType_AndReturnsTheStoredResource()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.Created, """{ "resourceType": "Patient", "id": "123" }""");

        // Act
        var result = await _service.CreateResourceAsync(TestFhirResources.TelemedicinePatient(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal($"{BaseUrl}Patient", request.Uri.ToString());
        Assert.Equal(FhirMediaTypes.Json, request.ContentType);
        Assert.Contains("Prefer: return=representation", request.Headers);
        Assert.DoesNotContain("If-None-Exist", request.Headers);
        Assert.Contains("\"resourceType\": \"Patient\"", request.Body);
        Assert.Equal("123", result.Value.Id);
    }

    [Fact]
    public async Task CreateResourceAsync_SendsIfNoneExist_ForAConditionalCreate()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Patient", "id": "123" }""");

        // Act
        await _service.CreateResourceAsync(
            TestFhirResources.TelemedicinePatient(),
            $"identifier={TestFhirResources.CprSystem}|2512489996",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains($"If-None-Exist: identifier={TestFhirResources.CprSystem}|2512489996", Assert.Single(_handler.Requests).Headers);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsValidationError_WithoutSending_WhenTheResourceBreaksItsContract()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { Subject = null };

        // Act
        var result = await _service.CreateResourceAsync(observation, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("Observation.subject", result.Error?.Message);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task PostTransactionAsync_PostsTheBundleToTheBaseUrl()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "Bundle", "type": "transaction-response" }""");
        var transaction = new Bundle
        {
            Type = BundleType.Transaction,
            Entry =
            [
                new BundleEntry
                {
                    FullUrl = "urn:uuid:5f6c0b7e-6f1c-4d0f-9a51-0d3f5f0a1b2c",
                    Resource = TestFhirResources.TelemedicinePatient(),
                    Request = new BundleEntryRequest { Method = HttpVerb.Post, Url = "Patient" }
                }
            ]
        };

        // Act
        var result = await _service.PostTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl, request.Uri.ToString());
        Assert.Contains("\"method\": \"POST\"", request.Body);
        Assert.Equal(BundleType.TransactionResponse, result.Value.Type);
    }

    [Fact]
    public async Task PostTransactionAsync_ReturnsValidationError_WithoutSending_WhenAnEntryBreaksItsContract()
    {
        // Arrange
        var transaction = new Bundle
        {
            Type = BundleType.Transaction,
            Entry = [new BundleEntry { Resource = TestFhirResources.TelemedicinePatient(), Request = null }]
        };

        // Act
        var result = await _service.PostTransactionAsync(transaction, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsTheOutcomeIssues_WhenTheServerRejectsTheResource()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.UnprocessableEntity, """
            {
              "resourceType": "OperationOutcome",
              "issue": [{ "severity": "error", "code": "required", "diagnostics": "Observation.effective[x] is required" }]
            }
            """);

        // Act
        var result = await _service.CreateResourceAsync(TestFhirResources.SleepObservation(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("[error] Observation.effective[x] is required", result.Error?.Message);
    }

    [Fact]
    public async Task GetCapabilityStatementAsync_ReturnsTheBody_WhenTheErrorIsNotAnOperationOutcome()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.BadGateway, "<html>Bad gateway</html>");

        // Act
        var result = await _service.GetCapabilityStatementAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
        Assert.Contains("<html>Bad gateway</html>", result.Error?.Message);
    }

    [Fact]
    public async Task GetCapabilityStatementAsync_ReturnsUnavailableError_WhenTheServerIsUnreachable()
    {
        // Arrange
        _handler.Throw(new HttpRequestException("Connection refused"));

        // Act
        var result = await _service.GetCapabilityStatementAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
        Assert.Contains("Connection refused", result.Error?.Message);
    }

    [Fact]
    public async Task GetCapabilityStatementAsync_ReturnsUnexpectedError_WhenTheBodyDoesNotFitTheModel()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, """{ "resourceType": "CapabilityStatement", "fhirVersion": "4.0.1" }""");

        // Act
        var result = await _service.GetCapabilityStatementAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
        Assert.Contains("does not fit the CapabilityStatement model", result.Error?.Message);
    }

    [Fact]
    public async Task GetCapabilityStatementAsync_ReturnsUnexpectedError_WhenTheBodyIsEmpty()
    {
        // Arrange
        _handler.RespondWith(HttpStatusCode.OK, "");

        // Act
        var result = await _service.GetCapabilityStatementAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
        Assert.Contains("empty body", result.Error?.Message);
    }
}
