using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Validation;
using Hl7Hackathon.Framework.Mappers;
using Hl7Hackathon.TestHelpers;

namespace Hl7Hackathon.UnitTests.Mappers;

[Trait("Category", "Unit")]
public class ObservationMapperTests
{
    private const string PatientReference = "Patient/7";
    private const string EpisodeOfCareReference = "EpisodeOfCare/42";

    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset _end = new(2026, 10, 7, 0, 0, 0, TimeSpan.FromHours(2));

    [Fact]
    public void ToFhir_BuildsASleepObservationThatMeetsTheContract()
    {
        // Arrange
        var request = CreateRequest(ObservationKindModel.Sleep, value: 7.5m);

        // Act
        var observation = ObservationMapper.ToFhir(
            request,
            ObservationKind.Sleep,
            ObservationKind.Sleep.Code!,
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        Assert.Empty(FhirModelValidator.Validate(observation));
        Assert.True(observation.ClaimsProfile(Telemedicine.Profiles.SleepObservation));
        Assert.Equal("248263006", observation.Code.Coding?[0].Code);
        Assert.Equal(PatientReference, observation.Subject?.Reference);
        Assert.Equal(PatientReference, Assert.Single(observation.Performer!).Reference);
        Assert.Equal("2026-10-06T00:00:00+02:00", observation.EffectivePeriod?.Start);
        Assert.Equal("2026-10-07T00:00:00+02:00", observation.EffectivePeriod?.End);
        Assert.Equal(7.5m, observation.ValueQuantity?.Value);
        Assert.Equal("Hours", observation.ValueQuantity?.Unit);
        Assert.Same(ObservationStatus.Final, observation.Status);
    }

    [Fact]
    public void ToFhir_LinksTheObservationToItsEpisodeOfCare()
    {
        // Arrange
        var request = CreateRequest(ObservationKindModel.Sleep, value: 7m);

        // Act
        var observation = ObservationMapper.ToFhir(
            request,
            ObservationKind.Sleep,
            ObservationKind.Sleep.Code!,
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        var extension = Assert.Single(observation.GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl));
        Assert.Equal(EpisodeOfCareReference, extension.ValueReference?.Reference);
    }

    [Fact]
    public void ToFhir_BuildsAPhysicalActivityObservationThatMeetsTheContract()
    {
        // Arrange
        var request = CreateRequest(ObservationKindModel.PhysicalActivity, value: 45m) with { ActivityCode = "129006008" };

        // Act
        var observation = ObservationMapper.ToFhir(
            request,
            ObservationKind.PhysicalActivity,
            ActivityCodes.Walking,
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        Assert.Empty(FhirModelValidator.Validate(observation));
        Assert.True(observation.ClaimsProfile(Telemedicine.Profiles.PhysicalActivityObservation));
        Assert.Equal("129006008", observation.Code.Coding?[0].Code);
        Assert.Equal("Minutes", observation.ValueQuantity?.Unit);
    }

    [Fact]
    public void ToFhir_SendsNoValue_WhenAPhysicalActivityObservationHasNone()
    {
        // Arrange
        var request = CreateRequest(ObservationKindModel.PhysicalActivity, value: null) with { ActivityCode = "129006008" };

        // Act
        var observation = ObservationMapper.ToFhir(
            request,
            ObservationKind.PhysicalActivity,
            ActivityCodes.Walking,
            PatientReference,
            EpisodeOfCareReference);

        // Assert
        Assert.Empty(FhirModelValidator.Validate(observation));
        Assert.Null(observation.ValueQuantity);
    }

    [Theory]
    [InlineData(ObservationKindModel.Sleep, Telemedicine.Profiles.SleepObservation)]
    [InlineData(ObservationKindModel.PhysicalActivity, Telemedicine.Profiles.PhysicalActivityObservation)]
    public void ToFhirKind_FindsTheProfile(ObservationKindModel kind, string expectedProfile)
    {
        // Act
        var fhirKind = ObservationMapper.ToFhirKind(kind);

        // Assert
        Assert.Equal(expectedProfile, fhirKind?.Profile);
    }

    [Fact]
    public void ToFhirKind_ReturnsNull_WhenTheKindIsNotOneWeSend()
    {
        // Act
        var fhirKind = ObservationMapper.ToFhirKind((ObservationKindModel)99);

        // Assert
        Assert.Null(fhirKind);
    }

    [Fact]
    public void ToActivityCodeModels_OffersOnlyCodesTheValueSetNamesOutright()
    {
        // Act
        var activityCodes = ObservationMapper.ToActivityCodeModels();

        // Assert
        Assert.Equal(ActivityCodes.All.Count, activityCodes.Count);
        Assert.Contains(activityCodes, activity => activity is { Code: "129006008", Display: "Walking" });
        Assert.Contains(activityCodes, activity => activity.Code == "129016000");
        Assert.All(activityCodes, activity => Assert.NotEmpty(activity.Display));
    }

    [Fact]
    public void ToModel_MapsWhatTheServerStored()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation(PatientReference, EpisodeOfCareReference) with { Id = "99" };

        // Act
        var model = ObservationMapper.ToModel(observation);

        // Assert
        Assert.Equal("99", model.Id);
        Assert.Equal("final", model.Status);
        Assert.Equal(ObservationKindModel.Sleep, model.Kind);
        Assert.Equal("Duration of sleep", model.Code);
        Assert.Equal(PatientReference, model.PatientReference);
        Assert.Equal(EpisodeOfCareReference, model.EpisodeOfCareReference);
        Assert.Equal(7.5m, model.Value);
        Assert.Equal("Hours", model.Unit);
    }

    [Fact]
    public void ToModel_LeavesTheKindNull_WhenTheProfileIsNotOneWeKnow()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with
        {
            Id = "99",
            Meta = new Meta { Profile = ["http://example.org/StructureDefinition/Something"] }
        };

        // Act
        var model = ObservationMapper.ToModel(observation);

        // Assert
        Assert.Null(model.Kind);
    }

    private static CreateObservationModel CreateRequest(ObservationKindModel kind, decimal? value) => new()
    {
        EpisodeOfCareId = "42",
        Kind = kind,
        Start = _start,
        End = _end,
        Value = value
    };
}
