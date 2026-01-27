namespace ProDispatch.Examples.Console.Features.Users.Commands;

public class CreateUserCommand(string userName, string email) : ICommand, IValidatable
{
    public string UserName { get; } = userName;
    public string Email { get; } = email;

    public IEnumerable<string> Validate()
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!Email.Contains("@"))
            errors.Add("Email must be a valid email address");

        return errors;
    }
}
