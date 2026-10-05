using Hl7Hackathon.Core.Results;

namespace Hl7Hackathon.UnitTests.Core;

[Trait("Category", "Unit")]
public class ResultTests
{
    [Fact]
    public void ImplicitConversion_ReturnsSuccess_WhenGivenAValue()
    {
        // Act
        Result<string> result = "value";

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal("value", result.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsFailure_WhenGivenAnError()
    {
        // Arrange
        var error = Error.NotFound("missing");

        // Act
        Result<string> result = error;

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Value_Throws_WhenResultIsFailure()
    {
        // Arrange
        Result<string> result = Error.Validation("bad input");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("bad input", exception.Message);
    }

    [Fact]
    public void Success_AllowsANullValue()
    {
        // Act
        var result = Result<string?>.Success(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsFailure_ForAnUntypedResult()
    {
        // Act
        Result result = Error.Conflict("already exists");

        // Assert
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
    }
}
