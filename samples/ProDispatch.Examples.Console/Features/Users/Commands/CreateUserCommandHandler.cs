using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Dispatcher;
using ProDispatch.Examples.Console.Features.Users.Notifications;

namespace ProDispatch.Examples.Console.Features.Users.Commands;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IDispatcher _dispatcher;

    public CreateUserCommandHandler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public async Task HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var userId = Guid.NewGuid();

        System.Console.WriteLine($"[USER] Creating user: {command.UserName} ({command.Email})");

        // Simulate user creation
        await Task.Delay(100, cancellationToken);

        System.Console.WriteLine($"[USER] User created with ID: {userId}");

        await _dispatcher.PublishAsync(
            new UserCreatedNotification(userId, command.UserName),
            cancellationToken);
    }
}
