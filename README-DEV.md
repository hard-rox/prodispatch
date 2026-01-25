# Lightweight Dispatcher Implementation

This project demonstrates a **lightweight, in-process dispatcher** built with pure C# (no external dependencies).

## Overview

The implementation provides a clean separation of concerns using the following patterns:

- **Commands**: Actions that modify state (e.g., `CreateUserCommand`)
- **Queries**: Requests that retrieve data (e.g., `GetUserByIdQuery`)
- **Notifications**: Events that notify handlers (e.g., `UserCreatedNotification`)
- **Pipeline Behaviors**: Cross-cutting concerns like logging and validation

## Architecture

### Core Interfaces

#### Commands
```csharp
public interface ICommand { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

#### Queries
```csharp
public interface IQuery<TResult> { }

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

#### Notifications
```csharp
public interface INotification { }

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
```

#### Pipeline Behaviors
```csharp
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken,
        Func<TRequest, CancellationToken, Task<TResponse>> next);
}
```

### Dispatcher

The `InProcessDispatcher` orchestrates all request handling:

```csharp
public interface IDispatcher
{
    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
```

**Key Features:**
- Resolves handlers using `IServiceFactory` (can be wired to any DI container)
- Builds and executes pipeline behaviors in registration order
- Supports multiple handlers for notifications (fan-out pattern)

## Project Structure

```
Prodispatch/
├── Abstractions/
│   ├── Commands/
│   │   ├── ICommand.cs
│   │   └── ICommandHandler.cs
│   ├── Queries/
│   │   ├── IQuery.cs
│   │   └── IQueryHandler.cs
│   ├── Notifications/
│   │   ├── INotification.cs
│   │   └── INotificationHandler.cs
│   ├── Pipeline/
│   │   └── IPipelineBehavior.cs
│   ├── Validation/
│   │   └── IValidatable.cs
│   ├── Exceptions/
│   │   └── ValidationException.cs
│   ├── Unit/
│   │   └── Unit.cs
│   └── Dispatcher/
│       └── IDispatcher.cs
├── Dispatcher/
│   └── InProcessDispatcher.cs
├── ServiceFactory/
│   ├── IServiceFactory.cs
│   └── SimpleServiceFactory.cs
├── Behaviors/
│   ├── LoggingBehavior.cs
│   └── ValidationBehavior.cs
├── Features/
│   └── Users/
│       ├── Commands/
│       │   ├── CreateUserCommand.cs
│       │   └── CreateUserCommandHandler.cs
│       ├── Queries/
│       │   ├── GetUserByIdQuery.cs
│       │   └── GetUserByIdQueryHandler.cs
│       └── Notifications/
│           ├── UserCreatedNotification.cs
│           ├── UserCreatedNotificationHandler.cs
│           └── SendWelcomeEmailNotificationHandler.cs
└── Program.cs
```

## Pipeline Behaviors

### LoggingBehavior
Logs request details including:
- Request type name
- Start/end timestamps
- Duration in milliseconds
- Errors (if any)

### ValidationBehavior
Validates requests implementing `IValidatable`:
- Calls `Validate()` before executing the handler
- Throws `ValidationException` if errors are found
- Skips validation if the request doesn't implement `IValidatable`

## Usage Example

### 1. Define a Command
```csharp
public class CreateUserCommand : ICommand, IValidatable
{
    public CreateUserCommand(string userName, string email)
    {
        UserName = userName;
        Email = email;
    }

    public string UserName { get; }
    public string Email { get; }

    public IEnumerable<string> Validate()
    {
        // validation logic
    }
}
```

### 2. Implement the Handler
```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IDispatcher _dispatcher;

    public CreateUserCommandHandler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Create user logic
        // Publish notification
        await _dispatcher.PublishAsync(new UserCreatedNotification(...), cancellationToken);
    }
}
```

### 3. Wire Up in Program.cs
```csharp
var serviceFactory = new SimpleServiceFactory();

// Register handlers
serviceFactory.Register<ICommandHandler<CreateUserCommand>>(
    () => new CreateUserCommandHandler(dispatcher));

// Register behaviors
serviceFactory.Register(
    typeof(IPipelineBehavior<CreateUserCommand, object>),
    () => new LoggingBehavior<CreateUserCommand, object>());

var dispatcher = new InProcessDispatcher(serviceFactory);

// Send command
await dispatcher.SendAsync(new CreateUserCommand("john", "john@example.com"));
```

## Service Factory

The `SimpleServiceFactory` provides a lightweight DI container:

- Supports registration of multiple handlers (for notifications)
- Uses factory functions for lazy initialization
- Can be replaced with a full DI container (e.g., Microsoft.Extensions.DependencyInjection)

## Running the Demo

Build and run:
```bash
dotnet build
dotnet run
```

Output demonstrates:
1. ✅ Command execution with pipeline behaviors
2. ✅ Query execution with logging
3. ✅ Validation failure handling
4. ✅ Notification publishing to multiple handlers

## Key Design Decisions

1. **No External Dependencies**: Pure C# implementation using .NET reflection
2. **Generic Pipeline**: Single behavior interface supports both commands and queries
3. **Reflection-Based Handler Resolution**: Allows dynamic handler discovery
4. **Sequential Notification Handling**: Handlers are awaited in order (can be changed to parallel)
5. **Simple Service Factory**: Enables integration with any DI container

## Extension Points

- Replace `SimpleServiceFactory` with Microsoft.Extensions.DependencyInjection
- Add more pipeline behaviors (caching, performance monitoring, etc.)
- Implement `IValidatable` for custom validation
- Add event sourcing support in command handlers
- Implement saga patterns for complex workflows

## Best Practices

1. ✅ Keep commands simple and focused on a single action
2. ✅ Use queries for read operations (no side effects)
3. ✅ Implement `IValidatable` for command/query validation
4. ✅ Use notifications for loosely-coupled event handling
5. ✅ Register behaviors in a consistent order
6. ✅ Use cancellation tokens for long-running operations

---

**Note**: This is a learning implementation. For production applications, consider using the official [MediatR](https://github.com/jbogard/MediatR) library.
