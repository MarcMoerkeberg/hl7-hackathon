using Hl7Hackathon.Web.Common;

namespace Hl7Hackathon.UnitTests.Web;

[Trait("Category", "Unit")]
public class DisplayFormatTests
{
    [Fact]
    public void Date_WritesTheDayFirst()
    {
        // Act
        var formatted = DisplayFormat.Date(new DateOnly(1948, 12, 25));

        // Assert
        Assert.Equal("25-12-1948", formatted);
    }

    [Theory]
    [InlineData("1948-12-25", "25-12-1948")]
    [InlineData("2026-01-02", "02-01-2026")]
    public void FhirDate_WritesTheDayFirst(string value, string expected)
    {
        // Act
        var formatted = DisplayFormat.FhirDate(value);

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData("1948-12")]
    [InlineData("1948")]
    [InlineData("")]
    [InlineData(null)]
    public void FhirDate_KeepsAValueWithNoDayToShow(string? value)
    {
        // Act
        var formatted = DisplayFormat.FhirDate(value);

        // Assert
        Assert.Equal(value, formatted);
    }

    [Fact]
    public void FhirDateTime_WritesTheDayFirstWithTheTimeOfDay()
    {
        // Act
        var formatted = DisplayFormat.FhirDateTime("2026-10-06T08:30:00+02:00");

        // Assert
        Assert.Equal("06-10-2026 08:30", formatted);
    }

    [Fact]
    public void FhirDateTime_KeepsTheTimeZoneItWasWrittenIn()
    {
        // Arrange
        // The same instant written in two zones must keep each zone's wall clock, not the one this machine runs in.
        const string copenhagen = "2026-10-06T23:30:00+02:00";
        const string utc = "2026-10-06T21:30:00+00:00";

        // Act
        var formattedCopenhagen = DisplayFormat.FhirDateTime(copenhagen);
        var formattedUtc = DisplayFormat.FhirDateTime(utc);

        // Assert
        Assert.Equal("06-10-2026 23:30", formattedCopenhagen);
        Assert.Equal("06-10-2026 21:30", formattedUtc);
    }

    [Fact]
    public void FhirDateTime_FallsBackToTheDate_WhenThereIsNoTime()
    {
        // Act
        var formatted = DisplayFormat.FhirDateTime("1948");

        // Assert
        Assert.Equal("1948", formatted);
    }

    [Fact]
    public void Cpr_PutsADashBeforeTheLastFourDigits()
    {
        // Act
        var formatted = DisplayFormat.Cpr("2512489996");

        // Assert
        Assert.Equal("251248-9996", formatted);
    }

    [Theory]
    [InlineData("251248-9996")]
    [InlineData("25124899")]
    [InlineData("25124899961")]
    [InlineData("25124899xy")]
    [InlineData("")]
    [InlineData(null)]
    public void Cpr_LeavesAnythingThatIsNotTenDigitsAlone(string? cpr)
    {
        // Act
        var formatted = DisplayFormat.Cpr(cpr);

        // Assert
        Assert.Equal(cpr, formatted);
    }
}
