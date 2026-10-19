# Copilot Instructions for ProDispatch

## Build, test, and format

- The solution targets .NET 10. Build it with `dotnet build ProDispatch.slnx --configuration Release`.
- Run the full test suite with `dotnet test ProDispatch.slnx --configuration Release`.
- Run one test by filtering its fully qualified name, for example:
  `dotnet test tests/ProDispatch.Tests/ProDispatch.Tests.csproj --filter "FullyQualifiedName~SendAsync_CommandWithResult_ReturnsValue"`
- The test projects are `tests/ProDispatch.Tests` and `tests/ProDispatch.IntegrationTests`; use the corresponding project file to run only one project.
- Run the console sample with `dotnet run --project samples/ProDispatch.Examples.Console/ProDispatch.Examples.Console.csproj`; run the benchmarks with `dotnet run -c Release --project benchmarks/ProDispatch.Benchmarks/ProDispatch.Benchmarks.csproj`.
- There is no separate lint command configured. `CONTRIBUTING.md` asks contributors adding public APIs to run `dotnet format ProDispatch.slnx`; Release builds enable warnings-as-errors and the configured analyzers.

## Architecture

- `src/ProDispatch` is a lightweight in-process dispatcher. It references `Microsoft.Extensions.DependencyInjection.Abstractions` directly. Register it with `services.AddProDispatch(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly))` and inject `IDispatcher`.
- The API supports both MediatR-style requests (`IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>`) and CQRS requests. `ICommand`, `ICommand<TResult>`, and `IQuery<TResult>` extend the request API, so they can be sent through `IDispatcher.Send` as well as the `SendAsync` convenience methods.
- `InProcessDispatcher` resolves a handler for the concrete request type. It first tries `IRequestHandler<TRequest, TResponse>`, then falls back to the matching command or query handler interface. Keep handlers' `HandleAsync` signatures aligned with those interfaces and pass cancellation tokens through.
- `ICommand` represents a command with no result and uses `Unit` internally; its handler still returns `Task`. Unit-response pipelines also support behaviors registered with `object` as the response type for compatibility.
- Pipeline behaviors are resolved for the request/response type and wrap the handler. `ICommandPipelineBehavior` and `IQueryPipelineBehavior` restrict a behavior to matching requests; behaviors without either marker apply to all request types.
- `PublishAsync` resolves every handler for the notification type and awaits all handler tasks together. Notifications fan out within the process; completion order is not guaranteed.
- The `samples` projects show DI registration and end-to-end usage. Examples of logging, validation, and scoped behaviors are sample code, not built-in core behaviors.

## Repository-specific conventions

- Assembly scanning discovers concrete, closed handlers and their inherited contracts, not behaviors. Single-handler defaults use `TryAdd`; notification handlers and explicitly configured behaviors are deduplicated by service/implementation pair.
- Configure closed behaviors with `cfg.AddBehavior<TBehavior>()` and open generics with `cfg.AddOpenBehavior(typeof(Behavior<,>))`.
- Behaviors are registered in execution order: the first registered behavior wraps outermost (runs first on entry), and the last registered behavior runs closest to the handler. The order reverses while unwinding the response.
- Dispatchers and scanned handlers are transient and use the current `IServiceProvider`. Register application dependencies normally on `IServiceCollection`; resolve the dispatcher within the scope that owns those dependencies. Never build a provider inside `AddProDispatch`.
- To participate in validation, a request implements `IValidatable`; the sample `ValidationBehavior` collects its messages and throws `ValidationException`. Validation behavior itself is not automatically installed by the library.
- Both handlers and behaviors receive a `CancellationToken`; preserve it when calling the next behavior or performing asynchronous work.
