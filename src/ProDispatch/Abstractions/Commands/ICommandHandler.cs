namespace ProDispatch.Abstractions.Commands;

/// <summary>
/// Generic handler interface for commands.
/// </summary>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>Handles the specified command.</summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous operation.</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic handler interface for commands that return a result.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>Handles the specified command and returns a result.</summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
