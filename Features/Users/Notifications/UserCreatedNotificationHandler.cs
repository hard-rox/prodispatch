using Prodispatch.Abstractions.Notifications;
using Prodispatch.Features.Users.Notifications;

namespace Prodispatch.Features.Users.Notifications;

/// <summary>
/// Handler for UserCreatedNotification.
/// </summary>
public class UserCreatedNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[NOTIFICATION] User created event received: UserId={notification.UserId}, UserName={notification.UserName}");
        return Task.CompletedTask;
    }
}
