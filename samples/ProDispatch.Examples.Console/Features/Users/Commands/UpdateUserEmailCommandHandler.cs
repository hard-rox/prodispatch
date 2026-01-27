namespace ProDispatch.Examples.Console.Features.Users.Commands;

public class UpdateUserEmailCommandHandler : ICommandHandler<UpdateUserEmailCommand, bool>
{
    public Task<bool> HandleAsync(UpdateUserEmailCommand command, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[USER] Updating email for user {command.UserId} to {command.NewEmail}");

        var success = command.UserId != Guid.Empty;

        if (success)
        {
            System.Console.WriteLine($"[USER] Email successfully updated for user {command.UserId}");
        }
        else
        {
            System.Console.WriteLine("[USER] Failed to update email - user not found");
        }

        return Task.FromResult(success);
    }
}
