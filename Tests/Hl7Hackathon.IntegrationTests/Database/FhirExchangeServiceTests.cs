using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Database;

[Collection(IntegrationTestsCollection.Name)]
[Trait("Category", "Integration")]
public class FhirExchangeServiceTests
{
    private readonly PostgresFixture _fixture;

    public FhirExchangeServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InsertFhirExchangeAsync_RoundTripsAllColumns()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFhirExchangeService>();
        var fhirExchange = CreateFhirExchange();

        // Act
        var id = await service.InsertFhirExchangeAsync(fhirExchange, TestContext.Current.CancellationToken);
        var stored = await service.GetFhirExchangeByIdAsync(id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(stored);
        Assert.Equal(fhirExchange.Url, stored.Url);
        Assert.Equal(fhirExchange.StatusCode, stored.StatusCode);
        Assert.Equal(fhirExchange.RequestBody, stored.RequestBody);
        Assert.Equal(fhirExchange.ResponseBody, stored.ResponseBody);
        Assert.Equal(fhirExchange.OccurredAt, stored.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, stored.OccurredAt.Kind);
    }

    [Fact]
    public async Task GetRecentFhirExchangesAsync_ReturnsNewestFirst_WithoutBodies()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFhirExchangeService>();
        await service.DeleteAllFhirExchangesAsync(TestContext.Current.CancellationToken);
        var olderId = await service.InsertFhirExchangeAsync(CreateFhirExchange(minutesAgo: 10), TestContext.Current.CancellationToken);
        var newerId = await service.InsertFhirExchangeAsync(CreateFhirExchange(minutesAgo: 1), TestContext.Current.CancellationToken);

        // Act
        var recent = await service.GetRecentFhirExchangesAsync(2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([newerId, olderId], recent.Select(exchange => exchange.Id));
        Assert.All(recent, exchange => Assert.Null(exchange.ResponseBody));
    }

    [Fact]
    public async Task DeleteAllFhirExchangesAsync_EmptiesTheLog()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IFhirExchangeService>();
        await service.InsertFhirExchangeAsync(CreateFhirExchange(), TestContext.Current.CancellationToken);

        // Act
        var deletedCount = await service.DeleteAllFhirExchangesAsync(TestContext.Current.CancellationToken);
        var remaining = await service.GetRecentFhirExchangesAsync(10, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(deletedCount >= 1);
        Assert.Empty(remaining);
    }

    // Truncated to microseconds, the precision PostgreSQL stores.
    private static FhirExchange CreateFhirExchange(int minutesAgo = 0)
    {
        var occurredAt = DateTime.UtcNow.AddMinutes(-minutesAgo);

        return new FhirExchange
        {
            ServerName = "Test",
            Method = "POST",
            Url = "http://localhost/fhir/Patient",
            StatusCode = 201,
            RequestBody = """{"resourceType":"Patient"}""",
            ResponseBody = """{"resourceType":"Patient","id":"1"}""",
            DurationMs = 42,
            OccurredAt = occurredAt.AddTicks(-(occurredAt.Ticks % 10))
        };
    }
}
