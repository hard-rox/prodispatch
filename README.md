# ProDispatch

[![SonarQube Cloud](https://sonarcloud.io/images/project_badges/sonarcloud-dark.svg)](https://sonarcloud.io/summary/new_code?id=hard-rox_prodispatch)

[![Quality gate status](https://sonarcloud.io/api/project_badges/measure?project=hard-rox_prodispatch&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=hard-rox_prodispatch)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=hard-rox_prodispatch&metric=coverage)](https://sonarcloud.io/summary/new_code?id=hard-rox_prodispatch)

ProDispatch is a lightweight, in-process dispatcher inspired by MediatR. It supports commands, queries, notifications, and pluggable pipeline behaviors without requiring a heavy dependency injection container.

## Highlights
- **MediatR-Compatible API**: Use `IRequest`/`IRequestHandler` for seamless migration from MediatR
- **CQRS Support**: Dedicated `ICommand`/`IQuery` interfaces with their respective handlers
- **Flexible Pipeline Behaviors**: Scope behaviors to commands-only, queries-only, or all requests
- **Notifications**: Fan-out to multiple handlers with `INotification`
- **Native Dependency Injection**: Uses `IServiceCollection` and `IServiceProvider` through Microsoft.Extensions.DependencyInjection.Abstractions
- **Fully Tested**: Comprehensive test coverage with samples, integration tests, and benchmarks

## Getting Started
1) Build the solution:
```
dotnet build ProDispatch.slnx
```
2) Run the sample console app:
```
dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj
```
3) Run tests:
```
dotnet test ProDispatch.slnx
```
4) Run benchmarks:
```
dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj
```

## Project Layout
- Library: src/ProDispatch
- Sample: samples/ProDispatch.Examples.Console
- Tests: tests/ProDispatch.Tests and tests/ProDispatch.IntegrationTests
- Benchmarks: benchmarks/ProDispatch.Benchmarks
- Docs: docs (see getting-started, advanced-usage, performance, migration-from-mediatr)

## Usage

### Registration
```csharp
using Microsoft.Extensions.DependencyInjection;
using ProDispatch.Abstractions.Dispatcher;

services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});
```

Inject `IDispatcher` into application services or Minimal API endpoints. Request, command, query, and notification handlers are discovered automatically and registered as transient services. Application dependencies are registered normally in `IServiceCollection` and resolved from the caller's scope.

Pipeline behaviors are opt-in and run first-added outermost:
```csharp
services.AddProDispatch(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
```

`LoggingBehavior` and `ValidationBehavior` are examples in the sample projects, not built-in library services.

### MediatR-Style API
```csharp
// Define a request
public record GetUserQuery(Guid Id) : IRequest<User>;

// Define a handler
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, User>
{
    public Task<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // ... fetch user
    }
}

// Send the request
var user = await dispatcher.Send(new GetUserQuery(userId));
```

### CQRS-Style API
```csharp
// Commands
public record CreateUserCommand(string Name) : ICommand<Guid>;
public class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid> { }

// Queries
public record GetUserByIdQuery(Guid Id) : IQuery<User>;
public class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, User> { }

// Usage
var userId = await dispatcher.SendAsync(new CreateUserCommand("Alice"));
var user = await dispatcher.SendAsync(new GetUserByIdQuery(userId));
```

### Pipeline Behaviors with Scoping
```csharp
// Apply to all requests
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> { }

// Apply only to commands
public class TransactionBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>, 
    ICommandPipelineBehavior { }

// Apply only to queries
public class CachingBehavior<TRequest, TResponse> : 
    IPipelineBehavior<TRequest, TResponse>, 
    IQueryPipelineBehavior { }
```

See docs/getting-started.md for a full walkthrough and docs/advanced-usage.md for customization tips.

## Release and CI/CD
- GitHub Actions build, test, and Sonar analysis via .github/workflows/ci.yml
- Security scanning via .github/workflows/codeql.yml
- NuGet packaging/release automation via .github/workflows/release.yml

## Contributing
Please read CONTRIBUTING.md and CODE_OF_CONDUCT.md before opening issues or pull requests. Suggestions and improvements are welcome.
