using Prodispatch.Abstractions.Commands;
using Prodispatch.Abstractions.Notifications;
using Prodispatch.Abstractions.Queries;

namespace Prodispatch.Abstractions.Dispatcher;

/// <summary>
/// In-process dispatcher interface for sending commands, queries, and publishing notifications.
/// </summary>
public interface IDispatcher
{
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);

    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);

    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
