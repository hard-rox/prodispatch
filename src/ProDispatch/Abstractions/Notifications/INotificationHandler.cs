namespace ProDispatch.Abstractions.Notifications;

/// <summary>
/// Generic handler interface for notifications.
/// </summary>
public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    /// <summary>Handles the notification asynchronously.</summary>
    /// <param name="notification">Notification payload.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous operation.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
