using Prodispatch.Abstractions.Commands;
using Prodispatch.Abstractions.Dispatcher;
using Prodispatch.Features.Users.Notifications;

namespace Prodispatch.Features.Users.Commands;

/// <summary>
/// Handler for CreateUserCommand.
/// </summary>
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

        Console.WriteLine($"[USER] Creating user: {command.UserName} ({command.Email})");

        // Simulate user creation
        await Task.Delay(100, cancellationToken);

        Console.WriteLine($"[USER] User created with ID: {userId}");

        // Publish notification
        await _dispatcher.PublishAsync(
            new UserCreatedNotification(userId, command.UserName),
            cancellationToken);
    }
}
