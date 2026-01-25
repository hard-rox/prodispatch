using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Validation;

namespace ProDispatch.Examples.Console.Features.Users.Commands;

public class CreateUserCommand : ICommand, IValidatable
{
    public CreateUserCommand(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

    public string UserName { get; }
    public string Email { get; }

    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(UserName))
            errors.Add("UserName is required");

        if (string.IsNullOrWhiteSpace(Email))
            errors.Add("Email is required");
        else if (!Email.Contains("@"))
            errors.Add("Email must be a valid email address");

        return errors;
    }
}
