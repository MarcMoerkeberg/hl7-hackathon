using Hl7Hackathon.Fhir.Codes;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.TestHelpers;

namespace Hl7Hackathon.UnitTests.Fhir.Codes;

[Trait("Category", "Unit")]
public class ObservationKindTests
{
    [Fact]
    public void FromClaimedProfile_ReturnsTheKind_WhoseProfileTheResourceClaims()
    {
        // Act & Assert
        Assert.Same(ObservationKind.Sleep, ObservationKind.FromClaimedProfile(TestFhirResources.SleepObservation()));
        Assert.Same(ObservationKind.PhysicalActivity, ObservationKind.FromClaimedProfile(TestFhirResources.PhysicalActivityObservation()));
    }

    [Fact]
    public void FromClaimedProfile_ReturnsNull_WhenTheResourceClaimsNoKind()
    {
        // Arrange
        var observation = TestFhirResources.SleepObservation() with { Meta = new Meta { Profile = ["http://example.org/OtherProfile"] } };

        // Act
        var kind = ObservationKind.FromClaimedProfile(observation);

        // Assert
        Assert.Null(kind);
    }

    [Fact]
    public void Sleep_FixesTheSnomedDurationOfSleepCode()
    {
        // Act & Assert
        Assert.Equal("http://snomed.info/sct", ObservationKind.Sleep.Code?.System);
        Assert.Equal("248263006", ObservationKind.Sleep.Code?.Code);
    }
}
