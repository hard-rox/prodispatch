# Migration from MediatR

ProDispatch mirrors common MediatR concepts but stays minimal. Use this checklist when migrating.

## Concepts
- MediatR's IRequest/IRequestHandler -> ProDispatch ICommand/ICommandHandler or IQuery/IQueryHandler
- MediatR's INotification -> ProDispatch INotification
- Behaviors -> IPipelineBehavior<TRequest, TResponse>

## Steps
1) Replace IRequest/INotification interfaces with the equivalents.
2) Update handlers to expose HandleAsync(T, CancellationToken).
3) Register handlers and behaviors with SimpleServiceFactory (or your DI-backed IServiceFactory).
4) Swap IMediator for InProcessDispatcher.

## Pipeline Ordering
MediatR wraps behaviors in registration order; ProDispatch wraps in reverse. Register outer behaviors first.

## Validation
Use IValidatable and ValidationBehavior instead of pre/post processors.

## Notifications
Handlers are invoked and awaited together (fan-out). Keep handlers independent and idempotent.

## Packaging
ProDispatch ships as a single package without DI extensions. Keep DI concerns in your host application.
