namespace Hl7Hackathon.Core.Results;

/// <summary>What kind of failure an <see cref="Error"/> is.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unavailable,
    Unexpected
}
