using System.Net;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Mappers;
using Hl7Hackathon.Fhir.Models.DataTypes;
using Hl7Hackathon.Fhir.Models.OperationOutcomes;

namespace Hl7Hackathon.UnitTests.Fhir;

[Trait("Category", "Unit")]
public class FhirServerErrorTests
{
    private static readonly OperationOutcome _noIssues = new() { Issue = [] };

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.Validation)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorType.Validation)]
    [InlineData(HttpStatusCode.NotFound, ErrorType.NotFound)]
    [InlineData(HttpStatusCode.Gone, ErrorType.NotFound)]
    [InlineData(HttpStatusCode.Conflict, ErrorType.Conflict)]
    [InlineData(HttpStatusCode.PreconditionFailed, ErrorType.Conflict)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorType.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.Forbidden)]
    [InlineData(HttpStatusCode.BadGateway, ErrorType.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorType.Unavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout, ErrorType.Unavailable)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.Unexpected)]
    public void FromResponse_MapsTheHttpStatusToAnErrorType(HttpStatusCode statusCode, ErrorType expectedType)
    {
        // Arrange
        using var response = new HttpResponseMessage(statusCode);

        // Act
        var error = FhirServerError.FromResponse("GET Patient/1", response, _noIssues);

        // Assert
        Assert.Equal(expectedType, error.Type);
    }

    [Fact]
    public void FromResponse_ListsEveryIssue_WithSeverityAndLocation()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity);
        var outcome = new OperationOutcome
        {
            Issue =
            [
                new OperationOutcomeIssue
                {
                    Severity = "error",
                    Code = "required",
                    Diagnostics = "Observation.effective[x]: minimum required = 1, but only found 0",
                    Expression = ["Observation"]
                },
                new OperationOutcomeIssue { Severity = "warning", Code = "code-invalid", Details = new CodeableConcept { Text = "Unknown code" } }
            ]
        };

        // Act
        var error = FhirServerError.FromResponse("POST Observation", response, outcome);

        // Assert
        Assert.StartsWith("POST Observation returned 422 UnprocessableEntity:", error.Message);
        Assert.Contains("[error] Observation.effective[x]: minimum required = 1, but only found 0 at Observation", error.Message);
        Assert.Contains("[warning] Unknown code", error.Message);
    }

    [Fact]
    public void FromResponse_FallsBackToTheIssueCode_WhenThereIsNoText()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var outcome = new OperationOutcome { Issue = [new OperationOutcomeIssue { Severity = "error", Code = "processing" }] };

        // Act
        var error = FhirServerError.FromResponse("POST Patient", response, outcome);

        // Assert
        Assert.Contains("[error] processing", error.Message);
    }

    [Fact]
    public void FromResponse_OnlyNamesTheRequestAndStatus_WhenThereAreNoIssues()
    {
        // Arrange
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        // Act
        var error = FhirServerError.FromResponse("GET Patient/1", response, _noIssues);

        // Assert
        Assert.Equal("GET Patient/1 returned 404 NotFound", error.Message);
    }
}
