namespace Prodispatch.Abstractions.Notifications;

/// <summary>
/// Generic handler interface for notifications.
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
