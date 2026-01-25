using Prodispatch.Abstractions.Notifications;

namespace Prodispatch.Features.Users.Notifications;

/// <summary>
/// Notification published when a user is created.
/// </summary>
public class UserCreatedNotification : INotification
{
    public UserCreatedNotification(Guid userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }

    public Guid UserId { get; }
    public string UserName { get; }
}
