using ProDispatch.Examples.MinimalApi.Behaviors;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

// Setup service factory
SimpleServiceFactory serviceFactory = new();
InProcessDispatcher? dispatcher = null;

// Register handlers
serviceFactory.Register<ICommandHandler<CreateUser>>(() => new CreateUserHandler(dispatcher!));
serviceFactory.Register<IQueryHandler<GetUserById, UserDto>>(() => new GetUserByIdHandler());
serviceFactory.Register<INotificationHandler<UserCreated>>(() => new UserCreatedNotificationHandler());
serviceFactory.Register<INotificationHandler<UserCreated>>(() => new SendWelcomeEmailNotificationHandler());

// Register pipeline behaviors (outermost first)
// Behaviors for all requests
serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUser, object>),
    () => new LoggingBehavior<CreateUser, object>());

serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUser, object>),
    () => new TimingBehavior<CreateUser, object>());

// Behavior scoped to commands only
serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUser, object>),
    () => new CommandOnlyBehavior<CreateUser, object>());

// Behavior scoped to commands only (validation)
serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUser, object>),
    () => new ValidationBehavior<CreateUser, object>());

// Behaviors for query
serviceFactory.Register(
    typeof(IPipelineBehavior<GetUserById, UserDto>),
    () => new LoggingBehavior<GetUserById, UserDto>());

serviceFactory.Register(
    typeof(IPipelineBehavior<GetUserById, UserDto>),
    () => new TimingBehavior<GetUserById, UserDto>());

// Behavior scoped to queries only
serviceFactory.Register(
    typeof(IPipelineBehavior<GetUserById, UserDto>),
    () => new QueryOnlyBehavior<GetUserById, UserDto>());

// Create dispatcher instance
dispatcher = new(serviceFactory);

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithDescription("Check API health status");

// Create user endpoint
app.MapPost("/users", async (CreateUserRequest request) =>
{
    try
    {
        CreateUser command = new(request.UserName, request.Email);
        await dispatcher!.SendAsync(command);
        return Results.Created($"/users", request);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(new { errors = ex.Errors });
    }
    catch
    {
        return Results.StatusCode(500);
    }
})
    .WithName("CreateUser")
    .WithDescription("Create a new user with validation");

// Get user endpoint
app.MapGet("/users/{userId:guid}", async (Guid userId) =>
{
    try
    {
        GetUserById query = new(userId);
        UserDto user = await dispatcher!.SendAsync(query);
        return Results.Ok(user);
    }
    catch (InvalidOperationException)
    {
        return Results.NotFound(new { message = "User not found" });
    }
    catch
    {
        return Results.StatusCode(500);
    }
})
    .WithName("GetUser")
    .WithDescription("Retrieve user details by ID");

app.Run();

// Request DTOs
public record CreateUserRequest(string UserName, string Email);

// Feature namespace for users
namespace ProDispatch.Examples.MinimalApi.Features.Users
{
    // Commands and handlers would go here (see next files)
}
