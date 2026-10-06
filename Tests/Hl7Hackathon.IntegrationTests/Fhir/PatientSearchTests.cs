using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories.Interfaces;
using Hl7Hackathon.IntegrationTests.Collections;
using Hl7Hackathon.TestHelpers;
using Hl7Hackathon.TestHelpers.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Hl7Hackathon.IntegrationTests.Fhir;

/// <summary>
/// Searches a throwaway HAPI server for patients this test put there first.
/// </summary>
[Collection(FhirServerTestsCollection.Name)]
[Trait("Category", "FhirServer")]
public class PatientSearchTests
{
    private readonly FhirServerFixture _fixture;

    public PatientSearchTests(FhirServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SearchPatientsAsync_FindsThePatientByCpr()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IPatientFactory>();
        var cpr = await GivenAPatientOnTheServerAsync(scope.ServiceProvider);

        // Act
        var result = await factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = cpr },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        var patient = Assert.Single(result.Value.Patients);
        Assert.Equal(cpr, patient.Cpr);
        Assert.Equal("Berggren", patient.FamilyName);
        Assert.Equal("Nancy Ann", patient.GivenName);
        Assert.Equal("female", patient.Gender);
        Assert.Equal("1948-12-25", patient.BirthDate);
    }

    [Fact]
    public async Task SearchPatientsAsync_FindsThePatientByNameAndBirthDate()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IPatientFactory>();
        var cpr = await GivenAPatientOnTheServerAsync(scope.ServiceProvider);

        // Act
        var result = await factory.SearchPatientsAsync(
            new SearchPatientsModel { Name = "Berggren", BirthDate = new DateOnly(1948, 12, 25) },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Contains(result.Value.Patients, patient => patient.Cpr == cpr);
    }

    [Fact]
    public async Task SearchPatientsAsync_ReturnsNoPatients_WhenTheCprIsOnNobody()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IPatientFactory>();

        // Act
        var result = await factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "0000000000" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Empty(result.Value.Patients);
        Assert.Equal(0, result.Value.Total);
    }

    [Fact]
    public async Task SearchPatientsAsync_ReturnsValidationError_WhenNoCriterionIsGiven()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IPatientFactory>();

        // Act
        var result = await factory.SearchPatientsAsync(new SearchPatientsModel(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    // A fresh CPR per test, so tests sharing the container never find each other's patients.
    private static async Task<string> GivenAPatientOnTheServerAsync(IServiceProvider services)
    {
        var cpr = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
        var fhirService = services.GetRequiredService<IFhirService>();
        var created = await fhirService.CreateResourceAsync(
            TestFhirResources.TelemedicinePatient(cpr),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess, created.Error?.Message);

        return cpr;
    }
}
