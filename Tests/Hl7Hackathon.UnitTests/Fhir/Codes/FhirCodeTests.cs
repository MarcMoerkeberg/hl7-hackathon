using Hl7Hackathon.Fhir.Codes;

namespace Hl7Hackathon.UnitTests.Fhir.Codes;

[Trait("Category", "Unit")]
public class FhirCodeTests
{
    [Fact]
    public void All_ListsEveryDeclaredCode()
    {
        // Act
        var codes = ObservationStatus.All.Select(status => status.Code);

        // Assert
        Assert.Equal(
            ["registered", "preliminary", "final", "amended", "corrected", "cancelled", "entered-in-error", "unknown"],
            codes);
    }

    [Fact]
    public void TryFromCode_ReturnsTheDeclaredInstance()
    {
        // Act
        var found = EpisodeOfCareStatus.TryFromCode("entered-in-error", out var status);

        // Assert
        Assert.True(found);
        Assert.Same(EpisodeOfCareStatus.EnteredInError, status);
    }

    [Theory]
    [InlineData("Final")]
    [InlineData("FINAL")]
    [InlineData("done")]
    public void TryFromCode_ReturnsFalse_ForAnUnknownOrWronglyCasedCode(string code)
    {
        // Act
        var found = ObservationStatus.TryFromCode(code, out var status);

        // Assert
        Assert.False(found);
        Assert.Null(status);
    }

    [Fact]
    public void ToString_ReturnsTheCode()
    {
        // Act & Assert
        Assert.Equal("transaction-response", BundleType.TransactionResponse.ToString());
    }

    [Fact]
    public void ValueSets_KeepTheirCodesApart_WhenTheCodesAreTheSame()
    {
        // Act
        ObservationStatus.TryFromCode("cancelled", out var observationStatus);
        EpisodeOfCareStatus.TryFromCode("cancelled", out var episodeOfCareStatus);

        // Assert
        Assert.Same(ObservationStatus.Cancelled, observationStatus);
        Assert.Same(EpisodeOfCareStatus.Cancelled, episodeOfCareStatus);
    }
}
