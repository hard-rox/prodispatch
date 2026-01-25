namespace Prodispatch.Abstractions.Commands;

/// <summary>
/// Marker interface for commands.
/// </summary>
public interface ICommand
{
}

/// <summary>
/// Marker interface for commands that return a result.
/// </summary>
public interface ICommand<out TResult>
{
}
