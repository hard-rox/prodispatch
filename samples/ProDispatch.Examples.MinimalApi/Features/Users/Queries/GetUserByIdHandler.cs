namespace ProDispatch.Examples.MinimalApi.Features.Users.Queries;

public class GetUserByIdHandler : IQueryHandler<GetUserById, UserDto>
{
    // In-memory user store for demo
    private static readonly Dictionary<Guid, UserDto> Users = new()
    {
        [new Guid("00000000-0000-0000-0000-000000000001")] = new(
            new Guid("00000000-0000-0000-0000-000000000001"),
            "alice",
            "alice@example.com"),
        [new Guid("00000000-0000-0000-0000-000000000002")] = new(
            new Guid("00000000-0000-0000-0000-000000000002"),
            "bob",
            "bob@example.com"),
    };

    public Task<UserDto> HandleAsync(GetUserById query, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[QUERY] Fetching user with ID: {query.Id}");

        if (Users.TryGetValue(query.Id, out var user))
        {
            return Task.FromResult(user);
        }

        throw new InvalidOperationException($"User with ID {query.Id} not found");
    }
}
