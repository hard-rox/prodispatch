using ProDispatch.Abstractions.Queries;

namespace ProDispatch.Examples.Console.Features.Users.Queries;

public class GetUserByIdQuery : IQuery<UserDto>
{
    public GetUserByIdQuery(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
}

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
