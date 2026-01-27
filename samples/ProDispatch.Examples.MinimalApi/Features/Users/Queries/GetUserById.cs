namespace ProDispatch.Examples.MinimalApi.Features.Users.Queries;

public class GetUserById : IQuery<UserDto>
{
    public GetUserById(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; }
}

public record UserDto(Guid Id, string UserName, string Email);
