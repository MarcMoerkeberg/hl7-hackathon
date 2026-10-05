namespace Hl7Hackathon.Core.Results;

/// <summary>
/// Represents a typed outcome of an operation.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T _value;

    private Result(T value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
        _value = default!;
    }

    /// <summary>
    /// The value of a successful result.
    /// </summary>
    /// <remarks>
    /// Throws an <see cref="InvalidOperationException"/> if the result is a non-success.
    /// </remarks>
    public T Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"A failed result has no value: {Error.Message}");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value of the successful result.</param>
    /// <returns>A new instance of <see cref="Result{T}"/> representing a successful outcome.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    /// <returns>A new instance of <see cref="Result{T}"/> representing a failed outcome.</returns>
    public new static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
