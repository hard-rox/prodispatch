using ProDispatch.Examples.MinimalApi.Behaviors;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger
builder.Services.AddOpenApi();

builder.Services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(TimingBehavior<,>));
    cfg.AddOpenBehavior(typeof(CommandOnlyBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(QueryOnlyBehavior<,>));
});

WebApplication app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithDescription("Check API health status");

// Create user endpoint
app.MapPost("/users", async (CreateUserRequest request, IDispatcher dispatcher, CancellationToken cancellationToken) =>
{
    try
    {
        CreateUser command = new(request.UserName, request.Email);
        await dispatcher.SendAsync(command, cancellationToken);
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
app.MapGet("/users/{userId:guid}", async (Guid userId, IDispatcher dispatcher, CancellationToken cancellationToken) =>
{
    try
    {
        GetUserById query = new(userId);
        UserDto user = await dispatcher.SendAsync(query, cancellationToken);
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