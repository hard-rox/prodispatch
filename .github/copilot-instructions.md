# Copilot Instructions for Prodispatch

- Purpose: lightweight, in-process dispatcher framework (MediatR-like) with commands, queries, notifications, and pipeline behaviors. Core abstractions live under Abstractions/; see Dispatcher/InProcessDispatcher.cs for execution flow.
- Build/run: target is net10.0 (Prodispatch.csproj). Use `dotnet build` and `dotnet run` from repo root; no external NuGet deps. No tests are present.
- Composition: Program.cs (in samples) wires everything manually via ServiceFactory/SimpleServiceFactory.cs. Register handlers/pipeline behaviors before creating Dispatcher/InProcessDispatcher.cs; dispatcher instance is then passed into handlers that need it (e.g., CreateUserCommandHandler) via constructor.
- Handler resolution: InProcessDispatcher.cs uses reflection to locate HandleAsync on the appropriate handler interface. Missing HandleAsync will throw InvalidOperationException, so keep signatures `Task HandleAsync(T, CancellationToken)` or `Task<TResult> HandleAsync(T, CancellationToken)`.
- Pipeline behaviors: ServiceFactory returns behaviors in registration order, but dispatcher wraps them in reverse; last registered runs closest to the handler. Behaviors must expose HandleAsync(TRequest, CancellationToken, Func<TRequest, CancellationToken, Task<TResponse>> next). **Example behaviors (LoggingBehavior, ValidationBehavior) are in sample projects under Behaviors/, not in the core library.**
- Validation: Implement Abstractions/Validation/IValidatable.cs to participate in a validation behavior. The ValidationBehavior example (in samples) aggregates errors and throws Abstractions/Exceptions/ValidationException.cs on failure. Keep Validate() side-effect free and return readable messages.
- Notifications: PublishAsync fans out to all registered INotificationHandler<T>. Handlers are resolved via ServiceFactory and invoked sequentially but awaited together with Task.WhenAll; keep handlers idempotent and fast since they share the same process.
- Commands/queries: Command without result uses ICommand; with result uses ICommand<TResult>. Query handlers return TResult. Examples live in Features/Users/Commands and Features/Users/Queries; UpdateUserEmailCommand shows command-with-result pattern.
- ServiceFactory: SimpleServiceFactory supports multiple registrations per type. GetInstance returns the first registration; GetInstances returns all. When adding handlers, register the one you expect to be primary first, or adjust factory if you need different selection logic.
- Cross-cutting order: Register behaviors per request type. If you add caching, metrics, etc., decide order explicitly; outermost behavior should usually be logging/metrics, innermost should be validation or business rules.
- Demo data: GetUserByIdQueryHandler uses an in-memory dictionary keyed by Guid; UpdateUserEmailCommandHandler simulates success when Guid is non-empty; adjust these when integrating real data sources.
- Error handling: Dispatcher surfaces handler exceptions directly; there is no global retry/backoff. Wrap callers if you need resilience.
- Extensibility: To plug in a real DI container, implement ServiceFactory/IServiceFactory.cs accordingly and keep the dispatcher API surface unchanged. Ensure multiple registrations are preserved for notifications.
- Cancellation: All handlers/behaviors accept CancellationToken; honor it in new code, especially around delays or I/O.
- Console output: Example behaviors and demo handlers write to stdout; this is the primary feedback mechanism when running dotnet run. Keep new logs concise and structured (prefix with [LOG], [VALIDATION], [USER], etc.).

If anything here feels incomplete or inaccurate, tell me which sections to clarify and I’ll revise quickly.
