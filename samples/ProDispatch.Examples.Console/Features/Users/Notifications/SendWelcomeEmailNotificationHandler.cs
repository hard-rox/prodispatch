using ProDispatch.Abstractions.Notifications;

namespace ProDispatch.Examples.Console.Features.Users.Notifications;

public class SendWelcomeEmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[EMAIL] Sending welcome email to user: {notification.UserName}");
        return Task.CompletedTask;
    }
}
