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
- ICommand / ICommand<TResult>
- IQuery<TResult>
- INotification
- IPipelineBehavior<TRequest, TResponse>
- IDispatcher

## Minimal Setup
```csharp
var factory = new SimpleServiceFactory();
InProcessDispatcher? dispatcher = null;

factory.Register<ICommandHandler<CreateUser>>(() => new CreateUserHandler(dispatcher!));
factory.Register(typeof(IPipelineBehavior<CreateUser, object>), () => new ValidationBehavior<CreateUser, object>());

dispatcher = new InProcessDispatcher(factory);
await dispatcher.SendAsync(new CreateUser("alice", "alice@example.com"));
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
