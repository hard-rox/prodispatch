namespace ProDispatch.Abstractions.Dispatcher;

/// <summary>
/// In-process dispatcher interface for sending commands, queries, and publishing notifications.
/// </summary>
public interface IDispatcher
{
    /// <summary>Sends a command without a result.</summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous operation.</returns>
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    /// <summary>Sends a command and returns a result.</summary>
    /// <typeparam name="TResult">Command result type.</typeparam>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Command result.</returns>
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    /// <summary>Sends a query and returns its result.</summary>
    /// <typeparam name="TResult">Query result type.</typeparam>
    /// <param name="query">Query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Query result.</returns>
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>Publishes a notification to all registered handlers.</summary>
    /// <param name="notification">Notification payload.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous operation.</returns>
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
