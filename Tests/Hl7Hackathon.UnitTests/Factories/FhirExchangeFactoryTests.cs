using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class FhirExchangeFactoryTests
{
    private readonly IFhirExchangeService _fhirExchangeService = Substitute.For<IFhirExchangeService>();
    private readonly FhirExchangeFactory _factory;

    public FhirExchangeFactoryTests()
    {
        _factory = new FhirExchangeFactory(_fhirExchangeService, NullLogger<FhirExchangeFactory>.Instance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(FhirExchangeFactory.MaxCount + 1)]
    public async Task GetRecentFhirExchangesAsync_ReturnsValidationError_WhenCountIsOutOfRange(int count)
    {
        // Act
        var result = await _factory.GetRecentFhirExchangesAsync(count, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        await _fhirExchangeService.DidNotReceive().GetRecentFhirExchangesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecentFhirExchangesAsync_ReturnsMappedSummaries()
    {
        // Arrange
        _fhirExchangeService.GetRecentFhirExchangesAsync(10, Arg.Any<CancellationToken>())
            .Returns([CreateFhirExchange(id: 7)]);

        // Act
        var result = await _factory.GetRecentFhirExchangesAsync(10, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var summary = Assert.Single(result.Value);
        Assert.Equal(7, summary.Id);
        Assert.Equal("POST", summary.Method);
        Assert.Equal(201, summary.StatusCode);
    }

    [Fact]
    public async Task GetRecentFhirExchangesAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _fhirExchangeService.GetRecentFhirExchangesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.GetRecentFhirExchangesAsync(10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task GetFhirExchangeByIdAsync_ReturnsTheExchange_WithBodies()
    {
        // Arrange
        _fhirExchangeService.GetFhirExchangeByIdAsync(7, Arg.Any<CancellationToken>()).Returns(CreateFhirExchange(id: 7));

        // Act
        var result = await _factory.GetFhirExchangeByIdAsync(7, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("""{"resourceType":"Patient"}""", result.Value.RequestBody);
        Assert.Equal("""{"resourceType":"Patient","id":"1"}""", result.Value.ResponseBody);
    }

    [Fact]
    public async Task GetFhirExchangeByIdAsync_ReturnsNotFoundError_WhenMissing()
    {
        // Arrange
        _fhirExchangeService.GetFhirExchangeByIdAsync(99, Arg.Any<CancellationToken>()).Returns((FhirExchange?)null);

        // Act
        var result = await _factory.GetFhirExchangeByIdAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task GetFhirExchangeByIdAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _fhirExchangeService.GetFhirExchangeByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.GetFhirExchangeByIdAsync(7, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task ClearFhirExchangesAsync_ReturnsTheDeletedCount()
    {
        // Arrange
        _fhirExchangeService.DeleteAllFhirExchangesAsync(Arg.Any<CancellationToken>()).Returns(3);

        // Act
        var result = await _factory.ClearFhirExchangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
    }

    [Fact]
    public async Task ClearFhirExchangesAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _fhirExchangeService.DeleteAllFhirExchangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.ClearFhirExchangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private static FhirExchange CreateFhirExchange(long id) => new()
    {
        Id = id,
        ServerName = "HapiPublic",
        Method = "POST",
        Url = "https://hapi.fhir.org/baseR4/Patient",
        StatusCode = 201,
        RequestBody = """{"resourceType":"Patient"}""",
        ResponseBody = """{"resourceType":"Patient","id":"1"}""",
        DurationMs = 120,
        OccurredAt = new DateTime(2026, 10, 6, 9, 30, 0, DateTimeKind.Utc)
    };
}
