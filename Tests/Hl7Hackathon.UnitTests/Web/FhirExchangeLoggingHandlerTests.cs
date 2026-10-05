using System.Net;
using Hl7Hackathon.Core.Models.Configuration;
using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.UnitTests.Stubs;
using Hl7Hackathon.Web.Middleware;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace Hl7Hackathon.UnitTests.Web;

[Trait("Category", "Unit")]
public class FhirExchangeLoggingHandlerTests
{
    private const string PatientUrl = "https://fhir.example.org/baseR4/Patient";

    private readonly StubHttpMessageHandler _server = new();
    private readonly IFhirExchangeService _fhirExchangeService = Substitute.For<IFhirExchangeService>();
    private readonly HttpMessageInvoker _invoker;

    public FhirExchangeLoggingHandlerTests()
    {
        var appSettings = new AppSettings
        {
            DbConnectionString = "Host=localhost",
            FhirServer = new ServerConnection { Name = "HapiPublic", BaseUrl = "https://fhir.example.org/baseR4" }
        };
        var handler = new FhirExchangeLoggingHandler(Options.Create(appSettings), _fhirExchangeService, NullLogger<FhirExchangeLoggingHandler>.Instance)
        {
            InnerHandler = _server
        };

        _invoker = new HttpMessageInvoker(handler);
    }

    [Fact]
    public async Task SendAsync_StoresTheExchange_WithBothBodiesAndTheStatus()
    {
        // Arrange
        _server.RespondWith(HttpStatusCode.Created, """{"resourceType":"Patient","id":"1"}""");
        using var request = new HttpRequestMessage(HttpMethod.Post, PatientUrl) { Content = new StringContent("""{"resourceType":"Patient"}""") };

        // Act
        using var response = await _invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await _fhirExchangeService.Received(1).InsertFhirExchangeAsync(
            Arg.Is<FhirExchange>(exchange =>
                exchange.ServerName == "HapiPublic"
                && exchange.Method == "POST"
                && exchange.Url == PatientUrl
                && exchange.StatusCode == 201
                && exchange.RequestBody == """{"resourceType":"Patient"}"""
                && exchange.ResponseBody == """{"resourceType":"Patient","id":"1"}"""
                && exchange.Error == null
                && exchange.OccurredAt.Kind == DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_LeavesTheResponseReadable_AfterLoggingIt()
    {
        // Arrange
        _server.RespondWith(HttpStatusCode.OK, """{"resourceType":"Patient","id":"1"}""");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{PatientUrl}/1");

        // Act
        using var response = await _invoker.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("""{"resourceType":"Patient","id":"1"}""", body);
    }

    [Fact]
    public async Task SendAsync_StoresTheError_AndRethrows_WhenTheServerIsUnreachable()
    {
        // Arrange
        _server.Throw(new HttpRequestException("Connection refused"));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{PatientUrl}/1");

        // Act
        await Assert.ThrowsAsync<HttpRequestException>(() => _invoker.SendAsync(request, TestContext.Current.CancellationToken));

        // Assert
        await _fhirExchangeService.Received(1).InsertFhirExchangeAsync(
            Arg.Is<FhirExchange>(exchange => exchange.Error == "Connection refused" && exchange.StatusCode == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ReturnsTheResponse_WhenStoringTheExchangeFails()
    {
        // Arrange
        _server.RespondWith(HttpStatusCode.OK, """{"resourceType":"Patient","id":"1"}""");
        _fhirExchangeService.InsertFhirExchangeAsync(Arg.Any<FhirExchange>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{PatientUrl}/1");

        // Act
        using var response = await _invoker.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
