namespace ProDispatch.Examples.MinimalApi.Features.Users.Commands;

public class CreateUserHandler : ICommandHandler<CreateUser>
{
    private readonly IDispatcher _dispatcher;

    public CreateUserHandler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public async Task HandleAsync(CreateUser command, CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid();
        Console.WriteLine($"[USER] Creating user: {command.UserName} ({command.Email}) with ID: {userId}");

        // Simulate user creation
        await Task.Delay(50, cancellationToken);

        // Publish notification
        await _dispatcher.PublishAsync(
            new UserCreated(userId, command.UserName),
            cancellationToken);
    }
}

public record UserCreated(Guid UserId, string UserName) : INotification;

public class UserCreatedNotificationHandler : INotificationHandler<UserCreated>
{
    public Task HandleAsync(UserCreated notification, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[NOTIFICATION] User created: {notification.UserName} (ID: {notification.UserId})");
        return Task.CompletedTask;
    }
}
