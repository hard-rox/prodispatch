using Prodispatch.Abstractions.Commands;
using Prodispatch.Abstractions.Validation;

namespace Prodispatch.Features.Users.Commands;

/// <summary>
/// Command to update a user's email that returns success status.
/// </summary>
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
        var errors = new List<string>();

        if (UserId == Guid.Empty)
            errors.Add("UserId cannot be empty");

        if (string.IsNullOrWhiteSpace(NewEmail))
            errors.Add("NewEmail is required");
        else if (!NewEmail.Contains("@"))
            errors.Add("NewEmail must be a valid email address");

        return errors;
    }
}
