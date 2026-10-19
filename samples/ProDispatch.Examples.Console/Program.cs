using ProDispatch.Abstractions.Exceptions;
using ProDispatch.Examples.Console.Behaviors;
using ProDispatch.Examples.Console.Features.Users.Commands;
using ProDispatch.Examples.Console.Features.Users.Notifications;
using ProDispatch.Examples.Console.Features.Users.Queries;

ServiceCollection services = new();
services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
    cfg.AddOpenBehavior(typeof(QueryOnlyBehavior<,>));
});

using ServiceProvider provider = services.BuildServiceProvider();
using IServiceScope scope = provider.CreateScope();
IDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

Console.WriteLine("=== ProDispatch Demo ===");

// Demo 1: Send a command
try
{
    Console.WriteLine("--- Demo 1: Creating a User (Command) ---");
    CreateUserCommand createCommand = new("john_doe", "john@example.com");
    await dispatcher.SendAsync(createCommand);
    Console.WriteLine();
}
catch (ValidationException ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}

// Demo 2: Send a query
try
{
    Console.WriteLine("--- Demo 2: Fetching a User (Query) ---");
    GetUserByIdQuery getQuery = new(new("00000000-0000-0000-0000-000000000001"));
    UserDto user = await dispatcher.SendAsync(getQuery);
    Console.WriteLine($"Result: {user}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}

// Demo 3: Command with result
try
{
    Console.WriteLine("--- Demo 3: Updating User Email (Command with Result) ---");
    UpdateUserEmailCommand updateCommand = new(
        new("00000000-0000-0000-0000-000000000001"),
        "newemail@example.com");
    var result = await dispatcher.SendAsync(updateCommand);
    Console.WriteLine($"Update result: {result}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}

// Demo 4: Validation failure
try
{
    Console.WriteLine("--- Demo 4: Validation Failure ---");
    CreateUserCommand invalidCommand = new("jane_doe", "invalid-email");
    await dispatcher.SendAsync(invalidCommand);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Error caught: {ex.Message}\n");
}

Console.WriteLine("=== Demo Complete ===");