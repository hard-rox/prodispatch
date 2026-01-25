namespace ProDispatch.Abstractions.Validation;

/// <summary>
/// Interface for objects that can be validated.
/// </summary>
public interface IValidatable
{
    /// <summary>Runs validation and returns any error messages.</summary>
    /// <returns>Collection of validation error messages; empty when valid.</returns>
    IEnumerable<string> Validate();
}
