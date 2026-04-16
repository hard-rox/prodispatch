using ProDispatch.Abstractions.Requests;
using UnitType = ProDispatch.Abstractions.Unit.Unit;

namespace ProDispatch.Abstractions.Commands;

/// <summary>
/// Generic handler interface for commands.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, UnitType> where TCommand : ICommand
{
    /// <summary>Handles the specified command.</summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous operation.</returns>
    new Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>Handles the specified command.</summary>
    /// <param name="request">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Unit value.</returns>
    async Task<UnitType> IRequestHandler<TCommand, UnitType>.HandleAsync(TCommand request, CancellationToken cancellationToken)
    {
        await HandleAsync(request, cancellationToken);
        return UnitType.Value;
    }
}

/// <summary>
/// Generic handler interface for commands that return a result.
/// </summary>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>Handles the specified command and returns a result.</summary>
    /// <param name="command">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    new Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);

    /// <summary>Handles the specified command and returns a result.</summary>
    /// <param name="request">The command to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command result.</returns>
    Task<TResult> IRequestHandler<TCommand, TResult>.HandleAsync(TCommand request, CancellationToken cancellationToken) =>
        HandleAsync(request, cancellationToken);
}
