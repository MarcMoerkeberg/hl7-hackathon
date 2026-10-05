using System.Net;
using Hl7Hackathon.Core.Results;
using Hl7Hackathon.Fhir.Models.OperationOutcomes;

namespace Hl7Hackathon.Fhir.Mappers;

/// <summary>
/// A mapper for handling FHIR server errors.
/// </summary>
/// <remarks>For more information on the FHIR HTTP status codes <see href="https://hl7.org/fhir/R4/http.html#Status-Codes">the documentation</see>.</remarks>
internal static class FhirServerError
{
    /// <summary>
    /// Creates an <see cref="Error"/> for a non-successful FHIR response.
    /// </summary>
    /// <param name="requestDescription">A description of the request that resulted in the error (eg. "GET /Patient/123").</param>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="outcome">The FHIR operation outcome.</param>
    /// <returns>A new instance of <see cref="Error"/> describing any issues or errors.</returns>
    public static Error FromResponse(string requestDescription, HttpResponseMessage response, OperationOutcome outcome)
    {
        var message = ToMessage(requestDescription, response, outcome);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => Error.Validation(message),
            HttpStatusCode.NotFound or HttpStatusCode.Gone => Error.NotFound(message),
            HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed => Error.Conflict(message),
            HttpStatusCode.Unauthorized => Error.Unauthorized(message),
            HttpStatusCode.Forbidden => Error.Forbidden(message),
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => Error.Unavailable(message),
            _ => Error.Unexpected(message)
        };
    }

    /// <summary>
    /// Creates the message for an <see cref="Error"/>.
    /// </summary>
    /// <param name="requestDescription">A description of the request that resulted in the error (eg. "GET /Patient/123").</param>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="outcome">The FHIR operation outcome.</param>
    /// <returns>A string representing the error message.</returns>
    private static string ToMessage(string requestDescription, HttpResponseMessage response, OperationOutcome outcome)
    {
        var header = $"{requestDescription} returned {(int)response.StatusCode} {response.StatusCode}";
        var issues = outcome.Issue.Select(DescribeIssue).ToList();

        return issues.Count == 0
            ? header
            : $"{header}:{Environment.NewLine}{string.Join(Environment.NewLine, issues)}";
    }

    /// <summary>
    /// Creates an easily human-readable description of the <paramref name="issue"/>.
    /// </summary>
    /// <param name="issue">The operation outcome issue to describe.</param>
    /// <returns>A string describing the issue.</returns>
    private static string DescribeIssue(OperationOutcomeIssue issue)
    {
        var text = issue.Diagnostics ?? issue.Details?.Text ?? issue.Code;
        var location = issue.Expression is { Count: > 0 }
            ? $" at {string.Join(", ", issue.Expression)}"
            : string.Empty;

        return $"[{issue.Severity}] {text}{location}";
    }
}
