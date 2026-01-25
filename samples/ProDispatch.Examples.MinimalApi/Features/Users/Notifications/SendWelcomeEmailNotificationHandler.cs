namespace ProDispatch.Examples.MinimalApi.Features.Users.Notifications;

/// <summary>
/// Notification handler that simulates sending a welcome email to new users.
/// </summary>
public class SendWelcomeEmailNotificationHandler : INotificationHandler<UserCreated>
{
    /// <summary>
    /// Handles the UserCreated notification by sending a welcome email.
    /// </summary>
    public Task HandleAsync(UserCreated notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[EMAIL] Welcome email sent to {notification.UserName} ({notification.UserId})");
        return Task.CompletedTask;
    }
}
