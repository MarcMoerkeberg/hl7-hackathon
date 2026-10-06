using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class ObservationFactoryTests
{
    private const string PatientReference = "Patient/7";

    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset _end = new(2026, 10, 7, 0, 0, 0, TimeSpan.FromHours(2));

    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
    private readonly ObservationFactory _factory;

    public ObservationFactoryTests()
    {
        _factory = new ObservationFactory(_fhirService, NullLogger<ObservationFactory>.Instance);
    }

    [Fact]
    public void GetActivityCodes_OffersTheActivitiesFromTheValueSet()
    {
        // Act
        var activityCodes = _factory.GetActivityCodes();

        // Assert
        Assert.Equal(ActivityCodes.All.Count, activityCodes.Count);
        Assert.Contains(activityCodes, activity => activity.Code == "129006008");
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsTheCreatedSleepObservation()
    {
        // Arrange
        GivenTheEpisodeOfCareExists();
        GivenTheServerStoresTheObservation();

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7.5m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("99", result.Value.Id);
        Assert.Equal(ObservationKindModel.Sleep, result.Value.Kind);
    }

    [Fact]
    public async Task CreateObservationAsync_SendsTheSleepProfileWithItsFixedCodeAndUnit()
    {
        // Arrange
        GivenTheEpisodeOfCareExists();
        GivenTheServerStoresTheObservation();

        // Act
        await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7.5m),
            TestContext.Current.CancellationToken);

        // Assert
        var sent = CapturedObservation();
        Assert.True(sent.ClaimsProfile(Telemedicine.Profiles.SleepObservation));
        Assert.Equal("248263006", sent.Code.Coding?[0].Code);
        Assert.Equal("Hours", sent.ValueQuantity?.Unit);
        Assert.Equal(PatientReference, sent.Subject?.Reference);
        Assert.Equal("EpisodeOfCare/42", Assert.Single(sent.GetExtensions(Telemedicine.EpisodeOfCareExtensionUrl)).ValueReference?.Reference);
    }

    [Fact]
    public async Task CreateObservationAsync_SendsTheChosenActivityCode()
    {
        // Arrange
        GivenTheEpisodeOfCareExists();
        GivenTheServerStoresTheObservation();

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.PhysicalActivity, value: 45m) with { ActivityCode = "129016000" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.Error?.Message);
        var sent = CapturedObservation();
        Assert.True(sent.ClaimsProfile(Telemedicine.Profiles.PhysicalActivityObservation));
        Assert.Equal("129016000", sent.Code.Coding?[0].Code);
        Assert.Equal("Minutes", sent.ValueQuantity?.Unit);
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenTheKindIsNotOneWeSend()
    {
        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest((ObservationKindModel)99, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenThePeriodEndsBeforeItStarts()
    {
        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m) with { End = _start.AddDays(-1) },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenSleepHasNoValue()
    {
        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: null),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenTheValueIsNegative()
    {
        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: -1m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-in-the-value-set")]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenThePhysicalActivityCodeIsUnusable(string? activityCode)
    {
        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.PhysicalActivity, value: 45m) with { ActivityCode = activityCode },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateObservationAsync_PassesOnTheErrorFromReadingTheEpisodeOfCare()
    {
        // Arrange
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(Error.NotFound("EpisodeOfCare/42 was not found"));

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsValidationError_WhenTheEpisodeOfCareNamesNoPatient()
    {
        // Arrange
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.TelemedicineEpisodeOfCare() with
            {
                Id = "42",
                Patient = new ResourceReference { Display = "a patient with no reference" }
            });

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task CreateObservationAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        GivenTheEpisodeOfCareExists();
        _fhirService.CreateResourceAsync(Arg.Any<Observation>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Error.Validation("Observation does not meet its FHIR contract"));

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("Observation does not meet its FHIR contract", result.Error?.Message);
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsUnexpectedError_WhenTheServerReturnsNoId()
    {
        // Arrange
        GivenTheEpisodeOfCareExists();
        _fhirService.CreateResourceAsync(Arg.Any<Observation>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.SleepObservation(PatientReference));

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task CreateObservationAsync_ReturnsUnexpectedError_WhenTheFhirServiceThrows()
    {
        // Arrange
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("the request timed out"));

        // Act
        var result = await _factory.CreateObservationAsync(
            CreateRequest(ObservationKindModel.Sleep, value: 7m),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private void GivenTheEpisodeOfCareExists()
    {
        _fhirService.GetResourceByIdAsync<EpisodeOfCare>("42", Arg.Any<CancellationToken>())
            .Returns(TestFhirResources.TelemedicineEpisodeOfCare(PatientReference) with { Id = "42" });
    }

    private void GivenTheServerStoresTheObservation()
    {
        _fhirService.CreateResourceAsync(Arg.Any<Observation>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => TestFhirResources.SleepObservation(PatientReference) with { Id = "99" });
    }

    private Observation CapturedObservation()
    {
        return (Observation)_fhirService.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IFhirService.CreateResourceAsync))
            .GetArguments()[0]!;
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
