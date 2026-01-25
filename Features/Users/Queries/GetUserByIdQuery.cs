using Prodispatch.Abstractions.Queries;

namespace Prodispatch.Features.Users.Queries;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public class GetUserByIdQuery : IQuery<UserDto>
{
    public GetUserByIdQuery(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
}

/// <summary>
/// Data transfer object for user information.
/// </summary>
public class UserDto
{
    public UserDto(Guid id, string userName, string email)
    {
        Id = id;
        UserName = userName;
        Email = email;
    }

    public Guid Id { get; }
    public string UserName { get; }
    public string Email { get; }

    public override string ToString() => $"User(Id={Id}, UserName={UserName}, Email={Email})";
}
