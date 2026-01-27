# Getting Started

This guide walks through installing ProDispatch, wiring a dispatcher, and sending your first commands and queries.

## Install
Add the package reference (once published):
```
dotnet add package ProDispatch
```
Or reference the local project while developing:
```
dotnet add <your-project>.csproj reference ../src/ProDispatch/ProDispatch.csproj
```

## Core Types

### MediatR-Compatible API
- `IRequest<TResponse>` - Base interface for requests that return a response
- `IRequest` - Interface for requests without a response (inherits `IRequest<Unit>`)
- `IRequestHandler<TRequest, TResponse>` - Handler for requests with responses
- `IRequestHandler<TRequest>` - Handler for requests without responses

### CQRS-Specific API
- `ICommand` / `ICommand<TResult>` - Command interfaces (inherit from `IRequest`)
- `IQuery<TResult>` - Query interface (inherits from `IRequest<TResult>`)
- `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResult>` - Command handlers
- `IQueryHandler<TQuery, TResult>` - Query handler

### Other Core Types
- `INotification` - Notification/event marker
- `IPipelineBehavior<TRequest, TResponse>` - Pipeline behavior interceptor
- `ICommandPipelineBehavior` - Marker for behaviors that only apply to commands
- `IQueryPipelineBehavior` - Marker for behaviors that only apply to queries
- `IDispatcher` - Main dispatcher interface

## MediatR-Style Setup
```csharp
var factory = new SimpleServiceFactory();
InProcessDispatcher? dispatcher = null;

// Register using IRequest/IRequestHandler
factory.Register<IRequestHandler<GetUserQuery, User>>(() => new GetUserQueryHandler());
factory.Register(typeof(IPipelineBehavior<GetUserQuery, User>), () => new LoggingBehavior<GetUserQuery, User>());

dispatcher = new InProcessDispatcher(factory);
var user = await dispatcher.Send(new GetUserQuery(userId));
```

## CQRS-Style Setup
```csharp
var factory = new SimpleServiceFactory();
InProcessDispatcher? dispatcher = null;

factory.Register<ICommandHandler<CreateUser>>(() => new CreateUserHandler(dispatcher!));
factory.Register<IQueryHandler<GetUserByIdQuery, User>>(() => new GetUserByIdHandler());
factory.Register(typeof(IPipelineBehavior<CreateUser, object>), () => new ValidationBehavior<CreateUser, object>());

dispatcher = new InProcessDispatcher(factory);

// Commands
await dispatcher.SendAsync(new CreateUser("alice", "alice@example.com"));

// Queries
var user = await dispatcher.SendAsync(new GetUserByIdQuery(userId));
```

## Pipeline Behavior Scoping
```csharp
// This behavior applies to ALL requests
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

// This behavior applies ONLY to commands
public class TransactionBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    ICommandPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        CancellationToken cancellationToken, 
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        // Begin transaction
        var result = await next(request, cancellationToken);
        // Commit transaction
        return result;
    }
}

// This behavior applies ONLY to queries
public class CachingBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    IQueryPipelineBehavior
{
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        CancellationToken cancellationToken, 
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        // Check cache
        return await next(request, cancellationToken);
        // Store in cache
    }
}
```

## Samples
Run the sample console app:
```
dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj
```

## Next Steps
- Explore docs/advanced-usage.md for ordering behaviors and plugging in DI
- See docs/performance.md for benchmarking guidance
- Migrate from MediatR with docs/migration-from-mediatr.md
