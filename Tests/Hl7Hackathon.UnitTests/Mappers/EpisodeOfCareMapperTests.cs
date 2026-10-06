using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;
using Hl7Hackathon.Framework.Mappers;
using PatientEntity = Hl7Hackathon.Core.Models.Entities.Patient;

namespace Hl7Hackathon.UnitTests.Mappers;

[Trait("Category", "Unit")]
public class EpisodeOfCareMapperTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void ToCreateTransaction_BuildsATransactionThatMeetsTheContract()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var transaction = EpisodeOfCareMapper.ToCreateTransaction(request, EpisodeOfCareStatus.Active, CreatePatient());

        // Assert
        Assert.Empty(FhirModelValidator.Validate(transaction));
        Assert.Equal(BundleType.Transaction, transaction.Type);
        Assert.Equal(2, transaction.Entry?.Count);
    }

    [Fact]
    public void ToCreateTransaction_CreatesThePatientConditionallyOnTheirCpr()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var transaction = EpisodeOfCareMapper.ToCreateTransaction(request, EpisodeOfCareStatus.Active, CreatePatient());

        // Assert
        var patientEntry = transaction.Entry![0];
        Assert.Equal("Patient", patientEntry.Request?.Url);
        Assert.Equal(HttpVerb.Post, patientEntry.Request?.Method);
        Assert.Equal($"identifier={DkCore.CprSystem}|2512489996", patientEntry.Request?.IfNoneExist);
        Assert.StartsWith("urn:uuid:", patientEntry.FullUrl);

        var patient = Assert.IsType<Patient>(patientEntry.Resource);
        Assert.True(patient.ClaimsProfile(Telemedicine.Profiles.TelemedicinePatient));
        Assert.Equal(DkCore.CprSystem, patient.Identifier?[0].System);
        Assert.Equal(["Nancy", "Ann"], patient.Name?[0].Given);
        Assert.Equal(AdministrativeGender.Female, patient.Gender);
        Assert.Equal("1948-12-25", patient.BirthDate);
    }

    [Fact]
    public void ToCreateTransaction_PointsTheEpisodeOfCareAtThePatientEntry()
    {
        // Arrange
        var request = CreateRequest(end: new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.FromHours(1)));

        // Act
        var transaction = EpisodeOfCareMapper.ToCreateTransaction(request, EpisodeOfCareStatus.Planned, CreatePatient());

        // Assert
        var episodeOfCareEntry = transaction.Entry![1];
        Assert.Equal("EpisodeOfCare", episodeOfCareEntry.Request?.Url);

        var episodeOfCare = Assert.IsType<EpisodeOfCare>(episodeOfCareEntry.Resource);
        Assert.True(episodeOfCare.ClaimsProfile(Telemedicine.Profiles.TelemedicineEpisodeOfCare));
        Assert.Equal(transaction.Entry[0].FullUrl, episodeOfCare.Patient.Reference);
        Assert.Equal(EpisodeOfCareStatus.Planned, episodeOfCare.Status);
        Assert.Equal("2026-10-06T00:00:00+02:00", episodeOfCare.Period?.Start);
        Assert.Equal("2026-12-31T23:59:59+01:00", episodeOfCare.Period?.End);
    }

    [Fact]
    public void ToCreateTransaction_LeavesThePeriodOpen_WhenTheRequestHasNoEnd()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var transaction = EpisodeOfCareMapper.ToCreateTransaction(request, EpisodeOfCareStatus.Active, CreatePatient());

        // Assert
        var episodeOfCare = Assert.IsType<EpisodeOfCare>(transaction.Entry![1].Resource);
        Assert.Null(episodeOfCare.Period?.End);
    }

    [Theory]
    [InlineData(EpisodeOfCareStatusModel.Planned, "planned")]
    [InlineData(EpisodeOfCareStatusModel.Waitlist, "waitlist")]
    [InlineData(EpisodeOfCareStatusModel.Active, "active")]
    [InlineData(EpisodeOfCareStatusModel.OnHold, "onhold")]
    [InlineData(EpisodeOfCareStatusModel.Finished, "finished")]
    [InlineData(EpisodeOfCareStatusModel.Cancelled, "cancelled")]
    public void ToFhirStatus_ReturnsTheCodeFromTheValueSet(EpisodeOfCareStatusModel status, string expectedCode)
    {
        // Act
        var fhirStatus = EpisodeOfCareMapper.ToFhirStatus(status);

        // Assert
        Assert.Equal(expectedCode, fhirStatus?.Code);
    }

    [Fact]
    public void ToFhirStatus_ReturnsNull_WhenTheStatusIsNotOneWeSend()
    {
        // Act
        var fhirStatus = EpisodeOfCareMapper.ToFhirStatus((EpisodeOfCareStatusModel)99);

        // Assert
        Assert.Null(fhirStatus);
    }

    [Fact]
    public void ToModel_MapsWhatTheServerStored()
    {
        // Arrange
        var episodeOfCare = new EpisodeOfCare
        {
            Id = "42",
            Status = EpisodeOfCareStatus.Active,
            Patient = new ResourceReference { Reference = "Patient/7" },
            Period = new Period { Start = "2026-10-06T00:00:00+02:00", End = "2026-12-31T23:59:59+01:00" }
        };

        // Act
        var model = EpisodeOfCareMapper.ToModel(episodeOfCare);

        // Assert
        Assert.Equal("42", model.Id);
        Assert.Equal("active", model.Status);
        Assert.Equal("Patient/7", model.PatientReference);
        Assert.Equal("2026-10-06T00:00:00+02:00", model.Start);
        Assert.Equal("2026-12-31T23:59:59+01:00", model.End);
    }

    private static CreateEpisodeOfCareModel CreateRequest(DateTimeOffset? end = null) => new()
    {
        PatientId = 1,
        Status = EpisodeOfCareStatusModel.Active,
        Start = _start,
        End = end
    };

    private static PatientEntity CreatePatient() => new()
    {
        Id = 1,
        Cpr = "2512489996",
        GivenName = "Nancy Ann",
        FamilyName = "Berggren",
        Gender = "female",
        BirthDate = new DateTime(1948, 12, 25)
    };
}
