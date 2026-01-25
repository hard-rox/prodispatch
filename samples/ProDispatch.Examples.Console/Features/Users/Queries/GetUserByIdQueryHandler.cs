using ProDispatch.Abstractions.Queries;

namespace ProDispatch.Examples.Console.Features.Users.Queries;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private static readonly Dictionary<Guid, UserDto> Users = new()
    {
        [new Guid("00000000-0000-0000-0000-000000000001")] = new(
            new Guid("00000000-0000-0000-0000-000000000001"),
            "john_doe",
            "john@example.com"),
        [new Guid("00000000-0000-0000-0000-000000000002")] = new(
            new Guid("00000000-0000-0000-0000-000000000002"),
            "jane_smith",
            "jane@example.com"),
    };

    public Task<UserDto> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[QUERY] Fetching user with ID: {query.Id}");

        if (Users.TryGetValue(query.Id, out var user))
        {
            return Task.FromResult(user);
        }

        throw new InvalidOperationException($"User with ID {query.Id} not found");
    }
}
