using Hl7Hackathon.Fhir.Validation;

namespace Hl7Hackathon.UnitTests.Fhir.Validation;

[Trait("Category", "Unit")]
public class FhirDateAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("2026")]
    [InlineData("2026-10")]
    [InlineData("2026-10-06")]
    public void FhirDate_Accepts_AFullOrPartialDate(string? value)
    {
        // Act & Assert
        Assert.True(new FhirDateAttribute().IsValid(value));
    }

    [Theory]
    [InlineData("06-10-2026")]
    [InlineData("2026-13")]
    [InlineData("2026-10-32")]
    [InlineData("2026-10-06T10:00:00+02:00")]
    public void FhirDate_Rejects_AnythingElse(string value)
    {
        // Act & Assert
        Assert.False(new FhirDateAttribute().IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026")]
    [InlineData("2026-10-06")]
    [InlineData("2026-10-06T10:00:00+02:00")]
    [InlineData("2026-10-06T10:00:00.123Z")]
    public void FhirDateTime_Accepts_ADateOrADateAndTimeWithATimeZone(string? value)
    {
        // Act & Assert
        Assert.True(new FhirDateTimeAttribute().IsValid(value));
    }

    [Theory]
    [InlineData("2026-10-06T10:00:00")]
    [InlineData("2026-10-06 10:00:00+02:00")]
    [InlineData("2026-10-06T25:00:00Z")]
    public void FhirDateTime_Rejects_ATimeWithoutATimeZone_OrAnInvalidTime(string value)
    {
        // Act & Assert
        Assert.False(new FhirDateTimeAttribute().IsValid(value));
    }
}
