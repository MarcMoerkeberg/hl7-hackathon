namespace Hl7Hackathon.Core.Results;

/// <summary>
/// Represents an error that occurred during an operation.
/// </summary>
public sealed record Error(ErrorType Type, string Message)
{
    /// <summary>
    /// Creates a validation error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing a validation error.</returns>
    public static Error Validation(string message) => new(ErrorType.Validation, message);

    /// <summary>
    /// Creates a not found error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing a not found error.</returns>
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);

    /// <summary>
    /// Creates a conflict error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing a conflict error.</returns>
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);

    /// <summary>
    /// Creates an unauthorized error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing an unauthorized error.</returns>
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);

    /// <summary>
    /// Creates a forbidden error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing a forbidden error.</returns>
    public static Error Forbidden(string message) => new(ErrorType.Forbidden, message);

    /// <summary>
    /// Creates an unavailable error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing an unavailable error.</returns>
    public static Error Unavailable(string message) => new(ErrorType.Unavailable, message);

    /// <summary>
    /// Creates an unexpected error with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A new instance of <see cref="Error"/> representing an unexpected error.</returns>
    public static Error Unexpected(string message) => new(ErrorType.Unexpected, message);
}
