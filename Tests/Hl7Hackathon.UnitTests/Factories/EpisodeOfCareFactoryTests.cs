using Hl7Hackathon.Core.Models.Domain;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models;
using Hl7Hackathon.Fhir.Common;
using Hl7Hackathon.Fhir.Models.Bundles;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Services.Interfaces;
using Hl7Hackathon.Framework.Factories;
using Hl7Hackathon.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;
using PatientEntity = Hl7Hackathon.Core.Models.Entities.Patient;

namespace Hl7Hackathon.UnitTests.Factories;

[Trait("Category", "Unit")]
public class EpisodeOfCareFactoryTests
{
    private static readonly DateTimeOffset _start = new(2026, 10, 6, 0, 0, 0, TimeSpan.FromHours(2));

    private readonly IPatientService _patientService = Substitute.For<IPatientService>();
    private readonly IFhirService _fhirService = Substitute.For<IFhirService>();
    private readonly EpisodeOfCareFactory _factory;

    public EpisodeOfCareFactoryTests()
    {
        _factory = new EpisodeOfCareFactory(_patientService, _fhirService, NullLogger<EpisodeOfCareFactory>.Instance);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsTheCreatedEpisodeOfCare()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.PostTransactionAsync(Arg.Any<Bundle>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResponse(CreatedEpisodeOfCare()));

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("42", result.Value.Id);
        Assert.Equal("active", result.Value.Status);
        Assert.Equal("Patient/7", result.Value.PatientReference);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_SendsATransactionWithThePatientAndTheEpisodeOfCare()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.PostTransactionAsync(Arg.Any<Bundle>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResponse(CreatedEpisodeOfCare()));

        // Act
        await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        var transaction = (Bundle)_fhirService.ReceivedCalls().Single().GetArguments()[0]!;
        Assert.Equal(BundleType.Transaction, transaction.Type);
        Assert.IsType<Patient>(transaction.Entry?[0].Resource);
        Assert.IsType<EpisodeOfCare>(transaction.Entry?[1].Resource);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsValidationError_WhenTheStatusIsNotOneWeSend()
    {
        // Arrange
        GivenThePatientExists();

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(
            CreateRequest() with { Status = (EpisodeOfCareStatusModel)99 },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsValidationError_WhenTheEndIsBeforeTheStart()
    {
        // Arrange
        GivenThePatientExists();

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(
            CreateRequest() with { End = _start.AddDays(-1) },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsNotFoundError_WhenThePatientIsNotInTheDatabase()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((PatientEntity?)null);

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.PostTransactionAsync(Arg.Any<Bundle>(), Arg.Any<CancellationToken>())
            .Returns(Error.Validation("EpisodeOfCare does not meet its FHIR contract"));

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("EpisodeOfCare does not meet its FHIR contract", result.Error?.Message);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsUnexpectedError_WhenTheResponseHasNoEpisodeOfCare()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.PostTransactionAsync(Arg.Any<Bundle>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle { Type = BundleType.TransactionResponse });

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsUnexpectedError_WhenTheServerReturnsAnEpisodeOfCareWithoutAnId()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.PostTransactionAsync(Arg.Any<Bundle>(), Arg.Any<CancellationToken>())
            .Returns(TransactionResponse(CreatedEpisodeOfCare() with { Id = null }));

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task CreateEpisodeOfCareAsync_ReturnsUnexpectedError_WhenTheDatabaseThrows()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.CreateEpisodeOfCareAsync(CreateRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_ReturnsTheEpisodesOnTheServer()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.SearchResourcesAsync<EpisodeOfCare>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(SearchSet(CreatedEpisodeOfCare()));

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var episodeOfCare = Assert.Single(result.Value);
        Assert.Equal("42", episodeOfCare.Id);
        Assert.Equal("active", episodeOfCare.Status);
        Assert.Equal("Patient/7", episodeOfCare.PatientReference);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_ChainsTheSearchThroughThePatientsCpr()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.SearchResourcesAsync<EpisodeOfCare>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(SearchSet());

        // Act
        await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        var parameters = (IEnumerable<(string Name, string Value)>)_fhirService.ReceivedCalls().Single().GetArguments()[0]!;
        Assert.Equal(("patient.identifier", $"{DkCore.CprSystem}|2512489996"), Assert.Single(parameters));
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_SkipsEntriesThatAreNotEpisodesOfCare()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.SearchResourcesAsync<EpisodeOfCare>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle
            {
                Type = BundleType.Searchset,
                Entry =
                [
                    new BundleEntry { Resource = CreatedEpisodeOfCare() },
                    new BundleEntry { Resource = CreatedEpisodeOfCare() with { Id = null } },
                    new BundleEntry { Resource = new Patient { Id = "7" } }
                ]
            });

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("42", Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_ReturnsNothing_WhenTheServerHasNoneForThePatient()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.SearchResourcesAsync<EpisodeOfCare>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(new Bundle { Type = BundleType.Searchset, Total = 0 });

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_ReturnsNotFoundError_WhenThePatientIsNotInTheDatabase()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns((PatientEntity?)null);

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
        Assert.Empty(_fhirService.ReceivedCalls());
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_PassesOnTheFhirServiceError()
    {
        // Arrange
        GivenThePatientExists();
        _fhirService.SearchResourcesAsync<EpisodeOfCare>(Arg.Any<IEnumerable<(string, string)>>(), Arg.Any<CancellationToken>())
            .Returns(Error.Unavailable("could not reach the FHIR server"));

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unavailable, result.Error?.Type);
    }

    [Fact]
    public async Task GetEpisodesOfCareForPatientAsync_ReturnsUnexpectedError_WhenTheDatabaseThrows()
    {
        // Arrange
        _patientService.GetPatientByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db down"));

        // Act
        var result = await _factory.GetEpisodesOfCareForPatientAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorType.Unexpected, result.Error?.Type);
    }

    private static Bundle SearchSet(params EpisodeOfCare[] episodesOfCare) => new()
    {
        Type = BundleType.Searchset,
        Total = episodesOfCare.Length,
        Entry = episodesOfCare.Length == 0
            ? null
            : episodesOfCare.Select(episodeOfCare => new BundleEntry { Resource = episodeOfCare }).ToList()
    };

    private void GivenThePatientExists()
    {
        _patientService.GetPatientByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new PatientEntity
        {
            Id = 1,
            Cpr = "2512489996",
            GivenName = "Nancy Ann",
            FamilyName = "Berggren",
            Gender = "female",
            BirthDate = new DateTime(1948, 12, 25)
        });
    }

    private static CreateEpisodeOfCareModel CreateRequest() => new()
    {
        PatientId = 1,
        Status = EpisodeOfCareStatusModel.Active,
        Start = _start
    };

    private static EpisodeOfCare CreatedEpisodeOfCare() => new()
    {
        Id = "42",
        Status = EpisodeOfCareStatus.Active,
        Patient = new ResourceReference { Reference = "Patient/7" },
        Period = new Period { Start = "2026-10-06T00:00:00+02:00" }
    };

    private static Bundle TransactionResponse(EpisodeOfCare episodeOfCare) => new()
    {
        Type = BundleType.TransactionResponse,
        Entry =
        [
            new BundleEntry
            {
                Resource = new Patient { Id = "7" },
                Response = new BundleEntryResponse { Status = "201 Created", Location = "Patient/7/_history/1" }
            },
            new BundleEntry
            {
                Resource = episodeOfCare,
                Response = new BundleEntryResponse { Status = "201 Created", Location = "EpisodeOfCare/42/_history/1" }
            }
        ]
    };
}
