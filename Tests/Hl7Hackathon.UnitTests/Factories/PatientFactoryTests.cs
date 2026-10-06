using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using FhirPatient = Hl7Hackathon.Fhir.Models.Patient;
using PatientEntity = Hl7Hackathon.Core.Models.Entities.Patient;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class PatientFactoryTests
{
    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
    private readonly PatientFactory _factory;

    public PatientFactoryTests()
    {
        _factory = new PatientFactory(_patientService, _fhirService, NullLogger<PatientFactory>.Instance);
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
        _patientService.GetPatientByIdAsync(99, Arg.Any<CancellationToken>()).Returns((PatientEntity?)null);

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

    [Fact]
    public async Task SearchPatientsAsync_ReturnsTheMatchingPatients()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(SearchSet(CreateFhirPatient()));

        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "2512489996" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        var patient = Assert.Single(result.Value.Patients);
        Assert.Equal("7", patient.Id);
        Assert.Equal("2512489996", patient.Cpr);
        Assert.Equal("Nancy Ann", patient.GivenName);
        Assert.Equal("Berggren", patient.FamilyName);
        Assert.Equal("female", patient.Gender);
        Assert.Equal("1948-12-25", patient.BirthDate);
    }

    [Fact]
    public async Task SearchPatientsAsync_SearchesByIdentifierNameAndBirthDate()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(SearchSet());

        // Act
        await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "251248-9996", Name = " Berggren ", BirthDate = new DateOnly(1948, 12, 25) },
            TestContext.Current.CancellationToken);

        // Assert
        var parameters = CapturedSearchParameters();
        Assert.Contains(($"{DkCore.CprSystem}|2512489996"), parameters.Select(parameter => parameter.Value));
        Assert.Contains(("name", "Berggren"), parameters);
        Assert.Contains(("birthdate", "1948-12-25"), parameters);
        Assert.Contains(("_count", "20"), parameters);
    }

    [Fact]
    public async Task SearchPatientsAsync_CapsThePageSize_WhenMoreIsAskedFor()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(SearchSet());

        // Act
        await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Name = "Berggren", MaxResults = 5000 },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(("_count", "100"), CapturedSearchParameters());
    }

    [Fact]
    public async Task SearchPatientsAsync_SkipsEntriesThatAreNotPatients()
    {
        // Arrange
        var bundle = new Bundle
        {
            Type = BundleType.Searchset,
            Total = 2,
            Entry =
            [
                new BundleEntry { Resource = CreateFhirPatient() },
                new BundleEntry { Resource = new FhirPatient { Id = null } },
                new BundleEntry { Resource = new EpisodeOfCare
                {
                    Id = "1",
                    Status = EpisodeOfCareStatus.Active,
                    Patient = new ResourceReference { Reference = "Patient/7" },
                    Period = new Period { Start = "2026-10-06" }
                } }
            ]
        };
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(bundle);

        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Name = "Berggren" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("7", Assert.Single(result.Value.Patients).Id);
    }

    [Fact]
    public async Task SearchPatientsAsync_ReturnsNoPatients_WhenNothingMatches()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle { Type = BundleType.Searchset, Total = 0 });

        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "2512489996" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Patients);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "   ")]
    [InlineData("-", null)]
    public async Task SearchPatientsAsync_ReturnsValidationError_WhenNoCriterionIsGiven(string? cpr, string? name)
    {
        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = cpr, Name = name },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task SearchPatientsAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(Error.Unavailable("could not reach the FHIR server"));

        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "2512489996" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
    }

    [Fact]
    public async Task SearchPatientsAsync_ReturnsUnexpectedError_WhenTheFhirServiceThrows()
    {
        // Arrange
        _fhirService.SearchResourcesAsync<FhirPatient>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("the request timed out"));

        // Act
        var result = await _factory.SearchPatientsAsync(
            new SearchPatientsModel { Cpr = "2512489996" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private (string Name, string Value)[] CapturedSearchParameters()
    {
        var parameters = (IEnumerable<(string Name, string Value)>)_fhirService.ReceivedCalls().Single().GetArguments()[0]!;

        return parameters.ToArray();
    }

    private static FhirPatient CreateFhirPatient() => new()
    {
        Id = "7",
        Identifier = [new Identifier { System = DkCore.CprSystem, Value = "2512489996" }],
        Name = [new HumanName { Use = NameUse.Official, Family = "Berggren", Given = ["Nancy", "Ann"] }],
        Gender = AdministrativeGender.Female,
        BirthDate = "1948-12-25"
    };

    private static Bundle SearchSet(params FhirPatient[] patients) => new()
    {
        Type = BundleType.Searchset,
        Total = patients.Length,
        Entry = patients.Length == 0
            ? null
            : patients.Select(patient => new BundleEntry { Resource = patient }).ToList()
    };

    private static PatientEntity CreatePatient(long id) => new()
    {
        Id = id,
        Cpr = "2512489996",
        GivenName = "Nancy Ann",
        FamilyName = "Berggren",
        Gender = "female",
        BirthDate = new DateTime(1948, 12, 25)
    };
}
