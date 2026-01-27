# Migration from MediatR

ProDispatch offers **full MediatR compatibility** with `IRequest`/`IRequestHandler`, plus additional CQRS-specific interfaces for clearer domain modeling.

## API Compatibility

### MediatR API (Fully Supported)
ProDispatch provides 1:1 compatibility with MediatR's core interfaces:

```csharp
// MediatR patterns work as-is
public record GetUserQuery(Guid Id) : IRequest<User>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, User>
{
    public Task<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Send requests using either API
var user = await dispatcher.Send(new GetUserQuery(userId));  // MediatR-style
```

### CQRS Extensions (ProDispatch-Specific)
For teams that want explicit CQRS semantics:

```csharp
// Commands
public record CreateUserCommand(string Name) : ICommand<Guid>;
public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid> { }

// Queries
public record GetUserByIdQuery(Guid Id) : IQuery<User>;
public class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, User> { }

// Send using CQRS-specific methods
await dispatcher.SendAsync(new CreateUserCommand("Alice"));
```

**Note**: `ICommand`, `IQuery`, and their handlers all inherit from `IRequest`/`IRequestHandler`, so you can mix approaches.

## Migration Steps

### 1. Replace Package Reference
```xml
<!-- Remove -->
<PackageReference Include="MediatR" Version="..." />

<!-- Add -->
<PackageReference Include="ProDispatch" Version="..." />
```

### 2. Update Namespaces
```csharp
// Replace
using MediatR;

// With
using ProDispatch.Abstractions.Requests;  // IRequest/IRequestHandler
using ProDispatch.Abstractions.Dispatcher;  // IDispatcher
using ProDispatch.Abstractions.Notifications;  // INotification
using ProDispatch.Abstractions.Pipeline;  // IPipelineBehavior
```

### 3. Dispatcher Interface Choices
ProDispatch provides two options:

**Option A: Migrate to IDispatcher (Recommended)**
```csharp
// Before (MediatR)
private readonly IMediator _mediator;

// After (ProDispatch)
private readonly IDispatcher _dispatcher;
```

**Option B: Use IMediator during transition (Deprecated)**
For gradual migration, use `IMediator` temporarily. It inherits from `IDispatcher` so all methods work identically:
```csharp
// Register both interfaces pointing to the same dispatcher instance
var dispatcher = new InProcessDispatcher(factory);
factory.Register<IDispatcher>(_ => dispatcher);
factory.Register<IMediator>(_ => dispatcher);  // Temporary bridge

// Existing code using IMediator continues to work
public class LegacyService
{
    private readonly IMediator _mediator;  // Still works
    
    public async Task DoWorkAsync()
    {
        await _mediator.Send(new MyRequest());  // Method calls unchanged
    }
}

// New code uses IDispatcher
public class ModernService
{
    private readonly IDispatcher _dispatcher;
    
    public async Task DoWorkAsync()
    {
        await _dispatcher.Send(new MyRequest());  // Same call, new interface
    }
}
```

> ⚠️ **Note**: `IMediator` is marked `[Obsolete]` and will be removed in a future version. Migrate to `IDispatcher` at your pace.

### 4. Update Method Names
```csharp
// MediatR
var result = await _mediator.Send(request);
await _mediator.Publish(notification);

// ProDispatch - MediatR-compatible API
var result = await _dispatcher.Send(request);
await _dispatcher.PublishAsync(notification);

// ProDispatch - CQRS-specific API (optional)
var result = await _dispatcher.SendAsync(command);  // for commands/queries
```

### 5. Update Handler Signatures (Optional)
ProDispatch uses `HandleAsync` instead of `Handle`:

```csharp
// MediatR
public Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)

// ProDispatch
public Task<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
```

**Note**: If you implement `IRequestHandler`, the `HandleAsync` method is required. If you implement `ICommandHandler` or `IQueryHandler`, they provide explicit implementations that delegate to your `HandleAsync` method.

### 6. Pipeline Behaviors
```csharp
// MediatR
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        return await next();
    }
}

// ProDispatch
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next)
    {
        return await next(request, cancellationToken);
    }
}
```

**Key difference**: ProDispatch passes `request` and `cancellationToken` to the `next` delegate explicitly.

### 7. Scoped Pipeline Behaviors (ProDispatch Feature)
ProDispatch allows you to scope behaviors to commands or queries only:

```csharp
// Apply ONLY to commands
public class TransactionBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    ICommandPipelineBehavior
{
    // Will only run for ICommand<T> requests
}

// Apply ONLY to queries
public class CachingBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>,
    IQueryPipelineBehavior
{
    // Will only run for IQuery<T> requests
}
```

## Pipeline Ordering
- **MediatR**: Behaviors wrap in registration order (first registered = outermost)
- **ProDispatch**: Behaviors wrap in **reverse** registration order (last registered = outermost)

To maintain the same order:
```csharp
// MediatR registration order
services.AddTransient<IPipelineBehavior<,>, LoggingBehavior<,>>();
services.AddTransient<IPipelineBehavior<,>, ValidationBehavior<,>>();

// ProDispatch equivalent (register in reverse)
factory.Register(typeof(IPipelineBehavior<,>), () => new ValidationBehavior<,>());
factory.Register(typeof(IPipelineBehavior<,>), () => new LoggingBehavior<,>());
```

## Validation
```csharp
// MediatR uses FluentValidation with IPipelineBehavior
// ProDispatch provides IValidatable interface

public record CreateUserCommand(string Name) : ICommand, IValidatable
{
    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return "Name is required";
    }
}

// Then use ValidationBehavior (example in samples)
```

## Notifications
```csharp
// MediatR
public class UserCreatedNotification : INotification { }
public class EmailHandler : INotificationHandler<UserCreatedNotification> 
{
    public Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken) { }
}

// ProDispatch
public class UserCreatedNotification : INotification { }
public class EmailHandler : INotificationHandler<UserCreatedNotification> 
{
    public Task HandleAsync(UserCreatedNotification notification, CancellationToken cancellationToken) { }
}
```

Handlers are invoked and awaited together (fan-out). Keep them idempotent and fast.

## DI Integration
- **MediatR**: Uses `AddMediatR()` with assembly scanning
- **ProDispatch**: Manual registration via `SimpleServiceFactory` or implement `IServiceFactory` for your DI container

```csharp
// ProDispatch with SimpleServiceFactory
var factory = new SimpleServiceFactory();
factory.Register<IRequestHandler<GetUserQuery, User>>(() => new GetUserQueryHandler());
var dispatcher = new InProcessDispatcher(factory);

// ProDispatch with custom IServiceFactory (e.g., Microsoft.Extensions.DependencyInjection)
public class DIServiceFactory : IServiceFactory
{
    private readonly IServiceProvider _provider;
    
    public object GetInstance(Type serviceType) => _provider.GetRequiredService(serviceType);
    public IEnumerable<object> GetInstances(Type serviceType) => _provider.GetServices(serviceType);
}
```

## Summary
- **Drop-in replacement** for MediatR core features
- Use `IRequest`/`IRequestHandler` for seamless migration
- Optionally adopt `ICommand`/`IQuery` for CQRS clarity
- Leverage scoped pipeline behaviors (`ICommandPipelineBehavior`, `IQueryPipelineBehavior`)
- Adjust for method name changes (`Handle` → `HandleAsync`) and behavior pipeline signature

## Transitioning from IMediator to IDispatcher

If using `IMediator` for backward compatibility, here's how to update over time:

```csharp
// Phase 1: Coexist - both interfaces work
public class UserService
{
    private readonly IMediator _mediator;  // Old way
    
    public UserService(IMediator mediator)
    {
        _mediator = mediator;
    }
}

// Phase 2: Migrate - gradually switch to IDispatcher
public class OrderService
{
    private readonly IDispatcher _dispatcher;  // New way
    
    public OrderService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }
}

// Phase 3: Complete - remove all IMediator references
// All services use IDispatcher only
```

The method calls remain identical across all phases, allowing you to migrate at your own pace:
```csharp
// All these work the same way
await _mediator.Send(request);
await _dispatcher.Send(request);
```

**When to migrate**:
- New code should always use `IDispatcher`
- Existing code can continue with `IMediator` until you're ready to update
- No breaking changes required - both interfaces coexist peacefully
