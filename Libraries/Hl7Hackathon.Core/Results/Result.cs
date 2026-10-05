using System.Diagnostics.CodeAnalysis;

namespace Hl7Hackathon.Core.Results;

/// <summary>
/// Represents the outcome of an operation.
/// </summary>
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    /// <summary>
    /// Details about the error if the result is a failure.
    /// </summary>
    /// <remarks>Is <see langword="null"/> when the result is successful.</remarks>
    public Error? Error { get; }

    /// <summary>
    /// <see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>
    /// <see langword="true"/> if the operation failed; otherwise, <see langword="false"/>.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A new instance of <see cref="Result"/> representing a successful outcome.</returns>
    public static Result Success() => new(null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A new instance of <see cref="Result"/> representing a failed outcome.</returns>
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}
