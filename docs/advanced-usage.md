# Advanced Usage

## Pipeline Behaviors
- Add closed behaviors with `cfg.AddBehavior<TBehavior>()` or open generic behaviors with `cfg.AddOpenBehavior(typeof(Behavior<,>))`.
- The first added behavior is **outermost**; the last is closest to the handler.
- For example, add logging, caching, then validation to execute them in that order before the handler, unwinding in reverse.
- Behaviors are not assembly-scanned. Repeated registration of the same service/implementation pair is deduplicated; use distinct implementation types for separate stages.
- **Note:** Example behaviors (LoggingBehavior, ValidationBehavior, CommandOnlyBehavior, QueryOnlyBehavior) are provided in the sample projects under `Behaviors/` directory. They demonstrate common patterns but are not part of the core library.

## Scoped Pipeline Behaviors
Control which requests a behavior applies to by implementing marker interfaces:

```csharp
// Applies to all requests (default)
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        CancellationToken cancellationToken, 
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        Console.WriteLine($"Executing {typeof(TRequest).Name}");
        return await next(request, cancellationToken);
    }
}

// Applies ONLY to commands
public class TransactionBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    ICommandPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        CancellationToken cancellationToken, 
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        // Begin transaction, execute, commit
        return await next(request, cancellationToken);
    }
}

// Applies ONLY to queries
public class CachingBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    IQueryPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        CancellationToken cancellationToken, 
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        // Check cache, execute if miss, store result
        return await next(request, cancellationToken);
    }
}
```

Scoping works automatically:
- Behaviors implementing `ICommandPipelineBehavior` only run for `ICommand` requests
- Behaviors implementing `IQueryPipelineBehavior` only run for `IQuery` requests
- Behaviors implementing neither run for **all** requests

## Validation
Implement IValidatable on requests to participate in a validation behavior (example ValidationBehavior provided in samples). Keep validation side-effect free and return readable messages.

```csharp
public record CreateUserCommand(string Name, string Email) : ICommand, IValidatable
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Name is required";
        
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
            yield return "Valid email is required";
    }
}
```

## DI Integration
ProDispatch directly references `Microsoft.Extensions.DependencyInjection.Abstractions`. Register services on `IServiceCollection` and resolve `IDispatcher` through the application's `IServiceProvider`:

```csharp
var services = new ServiceCollection();
services.AddScoped<IUserRepository, UserRepository>();
services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
```

Discovered handlers and the dispatcher are transient. Their dependencies are resolved from the caller's scope, so do not capture a dispatcher in a singleton when handlers need scoped dependencies. ProDispatch never builds or disposes an application provider.

Call `RegisterServicesFromAssembly` for each assembly containing handlers. Only concrete, closed handler implementations are scanned, including non-public types with public constructors. Open generic handler expansion is not supported. Scanning does not instantiate handlers.

Assembly inputs and handler registrations are deduplicated. Single-handler defaults use `TryAdd`, preserving an application registration made before `AddProDispatch`; later explicit registrations follow standard Microsoft DI override semantics. When multiple implementations exist for a single-handler contract, scanning selects the first by ordinal type full name. Prefer an explicit registration when choosing an override.

All distinct notification handlers are retained. Pipeline and notification collections preserve registration order, although notification completion order is not guaranteed.

## Notifications
PublishAsync fans out to all handlers. Keep handlers idempotent and fast; they run in-process and are awaited together.

```csharp
public record UserCreatedNotification(Guid UserId, string Email) : INotification;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send email
        return Task.CompletedTask;
    }
}

public class LogUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Log event
        return Task.CompletedTask;
    }
}

// Both handlers are discovered by RegisterServicesFromAssembly.

// Publish (both handlers execute concurrently)
await dispatcher.PublishAsync(new UserCreatedNotification(userId, email));
```

## Cross-cutting Examples
See the sample projects for example implementations:
- **LoggingBehavior** - logs request type and duration (all requests)
- **ValidationBehavior** - validates IValidatable requests (all requests)
- **TimingBehavior** - measures execution duration with Stopwatch (all requests)
- **CommandOnlyBehavior** - example scoped to commands only
- **QueryOnlyBehavior** - example scoped to queries only

## Unit Type
For commands without a result, use `Unit`:

```csharp
// Command without result
public record DeleteUserCommand(Guid UserId) : ICommand;

public class DeleteUserHandler : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        // Delete user
    }
}

// Internally returns Unit.Value
await dispatcher.SendAsync(new DeleteUserCommand(userId));
```

Unit is a struct with a singleton value used to represent "void" in async contexts.

Unit commands still accept closed `IPipelineBehavior<TCommand, object>` registrations for compatibility. Matching Unit-response behaviors run before the object-response group. If the same generic behavior implementation resolves for both response types, its Unit-response stage takes precedence so an open behavior does not run twice.
