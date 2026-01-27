# Advanced Usage

## Pipeline Behaviors
- Register behaviors per request type; the dispatcher wraps them in reverse registration order.
- Outer behaviors are registered **last**; innermost registered **first**.
- Typical order: handler -> validation -> caching -> logging/metrics (inner to outer).
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
Swap SimpleServiceFactory with your DI container of choice by implementing IServiceFactory:

```csharp
public class DIServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _provider;
    
    public DIServiceFactory(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public object GetInstance(Type serviceType) => 
        _provider.GetRequiredService(serviceType);
    
    public IEnumerable<object> GetInstances(Type serviceType) => 
        _provider.GetServices(serviceType);
}

// Usage
var services = new ServiceCollection();
services.AddTransient<IRequestHandler<GetUserQuery, User>, GetUserQueryHandler>();
var provider = services.BuildServiceProvider();
var dispatcher = new InProcessDispatcher(new DIServiceFactory(provider));
```

Preserve multiple registrations for notifications (use `GetServices` for fan-out).

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

// Register both handlers
factory.Register<INotificationHandler<UserCreatedNotification>>(() => new SendWelcomeEmailHandler());
factory.Register<INotificationHandler<UserCreatedNotification>>(() => new LogUserCreationHandler());

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
