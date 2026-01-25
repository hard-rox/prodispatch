namespace Prodispatch.Abstractions.Validation;

/// <summary>
/// Interface for objects that can be validated.
/// </summary>
public interface IValidatable
{
    IEnumerable<string> Validate();
}
