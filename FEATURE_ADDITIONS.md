# Recent Feature Additions to ProDispatch MinimalApi Sample

## Overview
Enhanced the ProDispatch.Examples.MinimalApi project with comprehensive pipeline behaviors, multiple notification handlers, and interactive API documentation browsing.

## New Files

### 1. TimingBehavior.cs
**Location:** `samples/ProDispatch.Examples.MinimalApi/Behaviors/TimingBehavior.cs`

A pipeline behavior that measures request execution duration using `System.Diagnostics.Stopwatch`. Logs elapsed milliseconds on both success and failure.

```csharp
public class TimingBehavior<TRequest> : IPipelineBehavior<TRequest, object>
```

**Usage:**
- Registered for CreateUser commands
- Registered for GetUserById queries
- Executes between LoggingBehavior and the handler

### 2. SendWelcomeEmailNotificationHandler.cs
**Location:** `samples/ProDispatch.Examples.MinimalApi/Features/Users/Notifications/SendWelcomeEmailNotificationHandler.cs`

A notification handler that simulates sending a welcome email when a user is created. Demonstrates multiple handlers for the same notification.

```csharp
public class SendWelcomeEmailNotificationHandler : INotificationHandler<UserCreated>
```

**Usage:**
- Registered alongside UserCreatedNotificationHandler
- Handlers are invoked sequentially and awaited together with Task.WhenAll

## Modified Files

### Program.cs
**Updates:**
1. Added `builder.Services.AddOpenApi()` for OpenAPI/Swagger support
2. Registered additional notification handler: `SendWelcomeEmailNotificationHandler`
3. Added `TimingBehavior<TRequest>` registrations:
   - For CreateUser command
   - For GetUserById query
4. Added Scalar integration:
   - `app.MapOpenApi()` - exposes OpenAPI schema
   - `app.MapScalarApiReference()` - serves interactive API documentation
5. Enhanced endpoint descriptions with `.WithDescription()`
6. Removed deprecated `.WithOpenApi()` calls (net10.0 deprecation)

**New Pipeline Order (CreateUser Command):**
1. LoggingBehavior (outermost)
2. ValidationBehavior
3. TimingBehavior
4. CreateUserHandler (innermost)

**New Pipeline Order (GetUserById Query):**
1. LoggingBehavior (outermost)
2. TimingBehavior
3. GetUserByIdHandler (innermost)

### GlobalUsings.cs
**Additions:**
- `global using Scalar.AspNetCore;` - for Scalar integration
- `global using ProDispatch.Abstractions.Exceptions;` - for ValidationException
- `global using ProDispatch.Examples.MinimalApi.Behaviors;` - for TimingBehavior
- `global using ProDispatch.Examples.MinimalApi.Features.Users.Notifications;` - for notification handlers

### ProDispatch.Examples.MinimalApi.csproj
**Package Additions:**
- `Microsoft.AspNetCore.OpenApi` (central version: 10.0.0)
- `Scalar.AspNetCore` (central version: 1.2.5)

### Directory.Packages.props
**Centralized Version Management:**
```xml
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageVersion Include="Scalar.AspNetCore" Version="1.2.5" />
```

### README.md
**Documentation Updates:**
- Added timing behavior to features list
- Added Scalar API Reference documentation (URL: `/scalar/v1`)
- Added "Viewing API Documentation" section
- Expanded "Architecture" section with detailed pipeline behavior ordering
- Documented notification handling with two handlers per event
- Added diagram of request flow through behaviors

## Features Enabled

### 1. Interactive API Documentation
Access Scalar UI at `http://localhost:5000/scalar/v1` when running the API. Features:
- Visual request/response exploration
- Built-in HTTP client for testing
- Schema validation

### 2. Timing Instrumentation
All requests now measure execution duration:
- Console output: `[TIMING] CreateUser completed in XXms`
- Console output: `[TIMING] GetUserById completed in XXms`
- Helps identify performance bottlenecks

### 3. Multi-Handler Notifications
The `UserCreated` notification now triggers two handlers:
1. `UserCreatedNotificationHandler` - logs creation
2. `SendWelcomeEmailNotificationHandler` - simulates email

Demonstrates:
- Multiple handlers for a single notification
- Sequential invocation
- Importance of handler idempotency

## Testing

All tests pass:
- ✅ ProDispatch.Tests: 6 tests passed
- ✅ ProDispatch.IntegrationTests: 1 test passed

Fixed FluentAssertions migration in integration tests (converted to xUnit Assert.*).

## Sample Output

When you create a user:
```
[LOG] Handling request: CreateUser
[VALIDATION] Validating CreateUser
[TIMING] CreateUser completed in 5ms
[USER] User 'john_doe' created with ID abc123
[NOTIFICATION] User 'john_doe' created notification received
[EMAIL] Welcome email sent to john_doe (abc123)
[LOG] Request CreateUser completed successfully
```

## Build Status

✅ **Build Status**: Clean release build with zero warnings/errors
- All 6 projects build successfully
- Central package management enforced
- No deprecated API usage
