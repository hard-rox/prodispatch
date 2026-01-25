# Advanced Usage

## Pipeline Behaviors
- Register behaviors per request type; the dispatcher wraps them in reverse registration order.
- Outer behaviors are registered first; innermost last.
- Typical order: logging/metrics -> caching -> validation -> handler.

## Validation
Implement IValidatable on requests to participate in ValidationBehavior. Keep validation side-effect free and return readable messages.

## DI Integration
Swap SimpleServiceFactory with your DI container of choice by implementing IServiceFactory. Preserve multiple registrations for notifications.

## Notifications
PublishAsync fans out to all handlers. Keep handlers idempotent and fast; they run in-process and are awaited together.

## Cross-cutting Examples
- MetricsBehavior to record timing
- RetryBehavior for transient faults
- CachingBehavior for queries

## Cancellation
All handlers and behaviors accept CancellationToken. Honor it around delays and I/O.
