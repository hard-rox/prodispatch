namespace ProDispatch.Examples.MinimalApi.Features.Users.Commands;

public class CreateUser : ICommand, IValidatable
{
    public CreateUser(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

    public string UserName { get; }
    public string Email { get; }

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(UserName))
            yield return "UserName is required";

        if (string.IsNullOrWhiteSpace(Email))
            yield return "Email is required";
        else if (!Email.Contains("@"))
            yield return "Email must be a valid email address";
    }
}
