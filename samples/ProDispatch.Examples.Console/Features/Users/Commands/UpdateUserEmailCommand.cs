namespace ProDispatch.Examples.Console.Features.Users.Commands;

public class UpdateUserEmailCommand : ICommand<bool>, IValidatable
{
    public UpdateUserEmailCommand(Guid userId, string newEmail)
    {
        UserId = userId;
        NewEmail = newEmail;
    }

    public Guid UserId { get; }
    public string NewEmail { get; }

    public IEnumerable<string> Validate()
    {
        List<string> errors = new();

        if (UserId == Guid.Empty)
            errors.Add("UserId cannot be empty");

        if (string.IsNullOrWhiteSpace(NewEmail))
            errors.Add("NewEmail is required");
        else if (!NewEmail.Contains("@"))
            errors.Add("NewEmail must be a valid email address");

        return errors;
    }
}
