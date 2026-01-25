using ProDispatch.Abstractions.Notifications;

namespace ProDispatch.Examples.Console.Features.Users.Notifications;

public class UserCreatedNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[NOTIFICATION] User created: UserId={notification.UserId}, UserName={notification.UserName}");
        return Task.CompletedTask;
    }
}
