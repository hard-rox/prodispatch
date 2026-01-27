namespace ProDispatch.Abstractions.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class.</summary>
    /// <param name="message">Validation failure message.</param>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with an inner exception.</summary>
    /// <param name="message">Validation failure message.</param>
    /// <param name="innerException">Inner exception.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ValidationException"/> class with validation errors.</summary>
    /// <param name="errors">Collection of validation error messages.</param>
    public ValidationException(IEnumerable<string> errors)
        : base($"Validation failed: {string.Join(", ", errors)}")
    {
        Errors = errors.ToList();
    }

    /// <summary>Gets the collection of validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; } = [];
}
