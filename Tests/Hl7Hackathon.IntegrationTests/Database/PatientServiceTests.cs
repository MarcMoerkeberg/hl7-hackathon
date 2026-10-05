using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.Services.Interfaces;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Database;

[Collection(IntegrationTestsCollection.Name)]
[Trait("Category", "Integration")]
public class PatientServiceTests
{
    private readonly PostgresFixture _fixture;

    public PatientServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetAllPatientsAsync_ReturnsSeededPatients()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPatientService>();

        // Act
        var patients = await service.GetAllPatientsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(4, patients.Count);
        var nancy = Assert.Single(patients, patient => patient.Cpr == "2512489996");
        Assert.Equal("Berggren", nancy.FamilyName);
        Assert.Equal(new DateTime(1948, 12, 25), nancy.BirthDate);
    }

    [Fact]
    public async Task GetPatientByIdAsync_ReturnsNull_WhenMissing()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPatientService>();

        // Act
        var patient = await service.GetPatientByIdAsync(long.MaxValue, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(patient);
    }
}
