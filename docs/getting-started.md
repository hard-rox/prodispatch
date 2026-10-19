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

## Registration

Register handlers by scanning their assembly. The same registration supports both request and CQRS APIs:

```csharp
using Microsoft.Extensions.DependencyInjection;
using ProDispatch.Abstractions.Dispatcher;

var services = new ServiceCollection();
services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

// MediatR-style requests
var user = await dispatcher.Send(new GetUserQuery(userId));
```

Console applications that build a provider also need the `Microsoft.Extensions.DependencyInjection` package. ASP.NET Core applications use `builder.Services.AddProDispatch(...)` and inject `IDispatcher` into endpoints or constructors; the host manages scopes and disposal.

Dispatchers and discovered handlers are transient. Register application dependencies such as repositories directly on `IServiceCollection`; scoped dependencies use the scope from which the dispatcher was resolved.

## CQRS Usage
```csharp
// Commands
await dispatcher.SendAsync(new CreateUser("alice", "alice@example.com"));

// Queries
var user = await dispatcher.SendAsync(new GetUserByIdQuery(userId));
```

## Registering Behaviors

Handlers and notifications are scanned automatically; behaviors are explicitly configured in execution order (first added is outermost):

```csharp
services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddBehavior<MyRequestBehavior>();
});
```

`AddOpenBehavior` accepts an open generic `IPipelineBehavior<TRequest, TResponse>` implementation. `AddBehavior` accepts a concrete closed implementation. The logging and validation implementations are provided in the samples.

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
