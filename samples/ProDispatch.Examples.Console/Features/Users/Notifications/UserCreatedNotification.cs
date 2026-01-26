namespace ProDispatch.Examples.Console.Features.Users.Notifications;

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
