using Prodispatch.Abstractions.Notifications;

namespace Prodispatch.Features.Users.Notifications;

/// <summary>
/// Another handler for UserCreatedNotification - sends a welcome email.
/// </summary>
public class SendWelcomeEmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[EMAIL] Sending welcome email to user: {notification.UserName}");
        return Task.CompletedTask;
    }
}
