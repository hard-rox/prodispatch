using Prodispatch.Abstractions.Commands;

namespace Prodispatch.Features.Users.Commands;

/// <summary>
/// Handler for UpdateUserEmailCommand.
/// </summary>
public class UpdateUserEmailCommandHandler : ICommandHandler<UpdateUserEmailCommand, bool>
{
    public Task<bool> HandleAsync(UpdateUserEmailCommand command, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[USER] Updating email for user {command.UserId} to {command.NewEmail}");

        // Simulate email update
        var success = command.UserId != Guid.Empty;

        if (success)
        {
            Console.WriteLine($"[USER] Email successfully updated for user {command.UserId}");
        }
        else
        {
            Console.WriteLine($"[USER] Failed to update email - user not found");
        }

        return Task.FromResult(success);
    }
}
