using Prodispatch.Abstractions.Commands;
using Prodispatch.Abstractions.Exceptions;
using Prodispatch.Abstractions.Dispatcher;
using Prodispatch.Abstractions.Notifications;
using Prodispatch.Abstractions.Pipeline;
using Prodispatch.Abstractions.Queries;
using Prodispatch.Behaviors;
using Prodispatch.Features.Users.Commands;
using Prodispatch.Features.Users.Notifications;
using Prodispatch.Features.Users.Queries;
using Prodispatch.Dispatcher;
using Prodispatch.ServiceFactory;

// Setup service factory
var serviceFactory = new SimpleServiceFactory();

// Create dispatcher first (it will be used for dependency injection)
InProcessDispatcher? dispatcher = null;

// Register handlers
serviceFactory.Register<ICommandHandler<CreateUserCommand>>(() => new CreateUserCommandHandler(dispatcher!));
serviceFactory.Register<ICommandHandler<UpdateUserEmailCommand, bool>>(() => new UpdateUserEmailCommandHandler());
serviceFactory.Register<IQueryHandler<GetUserByIdQuery, UserDto>>(() => new GetUserByIdQueryHandler());
serviceFactory.Register<INotificationHandler<UserCreatedNotification>>(() => new UserCreatedNotificationHandler());
serviceFactory.Register<INotificationHandler<UserCreatedNotification>>(() => new SendWelcomeEmailNotificationHandler());

// Register pipeline behaviors
serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUserCommand, object>),
    () => new LoggingBehavior<CreateUserCommand, object>());

serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUserCommand, object>),
    () => new ValidationBehavior<CreateUserCommand, object>());

serviceFactory.Register(
    typeof(IPipelineBehavior<UpdateUserEmailCommand, bool>),
    () => new LoggingBehavior<UpdateUserEmailCommand, bool>());

serviceFactory.Register(
    typeof(IPipelineBehavior<UpdateUserEmailCommand, bool>),
    () => new ValidationBehavior<UpdateUserEmailCommand, bool>());

serviceFactory.Register(
    typeof(IPipelineBehavior<GetUserByIdQuery, UserDto>),
    () => new LoggingBehavior<GetUserByIdQuery, UserDto>());

// Create dispatcher instance
dispatcher = new InProcessDispatcher(serviceFactory);

Console.WriteLine("=== Dispatcher Implementation Demo ===");

// Demo 1: Send a command
try
{
    Console.WriteLine("--- Demo 1: Creating a User (Command) ---");
    var createCommand = new CreateUserCommand("john_doe", "john@example.com");
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
    var getQuery = new GetUserByIdQuery(new Guid("00000000-0000-0000-0000-000000000001"));
    var user = await dispatcher.SendAsync(getQuery);
    Console.WriteLine($"Result: {user}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}

// Demo 3: Validation failure
try
{
// Demo 4: Command with result
try
{
    Console.WriteLine("--- Demo 4: Updating User Email (Command with Result) ---");
    var updateCommand = new UpdateUserEmailCommand(
        new Guid("00000000-0000-0000-0000-000000000001"),
        "newemail@example.com");
    var result = await dispatcher.SendAsync(updateCommand);
    Console.WriteLine($"Update result: {result}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}
    var invalidCommand = new CreateUserCommand("jane_doe", "invalid-email");
    await dispatcher.SendAsync(invalidCommand);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Error caught: {ex.Message}\n");
}

Console.WriteLine("=== Demo Complete ===");
