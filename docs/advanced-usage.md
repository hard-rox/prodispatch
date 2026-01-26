# Advanced Usage

## Pipeline Behaviors
- Register behaviors per request type; the dispatcher wraps them in reverse registration order.
- Outer behaviors are registered first; innermost last.
- Typical order: logging/metrics -> caching -> validation -> handler.
- **Note:** Example behaviors (LoggingBehavior, ValidationBehavior) are provided in the sample projects under `Behaviors/` directory. They demonstrate common patterns but are not part of the core library.

## Validation
Implement IValidatable on requests to participate in a validation behavior (example ValidationBehavior provided in samples). Keep validation side-effect free and return readable messages.

## DI Integration
Swap SimpleServiceFactory with your DI container of choice by implementing IServiceFactory. Preserve multiple registrations for notifications.

## Notifications
PublishAsync fans out to all handlers. Keep handlers idempotent and fast; they run in-process and are awaited together.

## Cross-cutting Examples
See the sample projects for example implementations:
- LoggingBehavior - logs request type and duration
- ValidationBehavior - validates IValidatable requests
- TimingBehavior - measures execution duration with Stopwatch
