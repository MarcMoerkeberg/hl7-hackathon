using Hl7Hackathon.Core.Models.Entities;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class PatientFactoryTests
{
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly PatientFactory _factory;

    public PatientFactoryTests()
    {
        _factory = new PatientFactory(_patientService, NullLogger<PatientFactory>.Instance);
    }

    [Fact]
    public async Task GetPatientsAsync_ReturnsMappedPatients()
    {
        // Arrange
        _patientService.GetAllPatientsAsync(Arg.Any<CancellationToken>()).Returns([CreatePatient(id: 1)]);

        // Act
        var result = await _factory.GetPatientsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var patient = Assert.Single(result.Value);
        Assert.Equal("2512489996", patient.Cpr);
        Assert.Equal(new DateOnly(1948, 12, 25), patient.BirthDate);
    }

    [Fact]
    public async Task GetPatientsAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _patientService.GetAllPatientsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.GetPatientsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task GetPatientByIdAsync_ReturnsThePatient_WhenFound()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreatePatient(id: 1));

        // Act
        var result = await _factory.GetPatientByIdAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Berggren", result.Value.FamilyName);
    }

    [Fact]
    public async Task GetPatientByIdAsync_ReturnsNotFoundError_WhenMissing()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        // Act
        var result = await _factory.GetPatientByIdAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task GetPatientByIdAsync_ReturnsUnexpectedError_WhenServiceThrows()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.GetPatientByIdAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private static Patient CreatePatient(long id) => new()
    {
        Id = id,
        Cpr = "2512489996",
        GivenName = "Nancy Ann",
        FamilyName = "Berggren",
        Gender = "female",
        BirthDate = new DateTime(1948, 12, 25)
    };
}
