# Minimal API Example

This sample demonstrates using ProDispatch in an ASP.NET Core minimal API application.

## Features

- **Health check endpoint** (`GET /health`): Returns service status
- **Create user endpoint** (`POST /users`): Sends CreateUser command with validation
- **Get user endpoint** (`GET /users/{userId}`): Executes GetUserById query
- **Pipeline behaviors**: Logging, validation, and timing on commands/queries
- **Notifications**: UserCreated event published and handled by multiple handlers
- **OpenAPI/Swagger**: Built-in documentation via `MapOpenApi()`
- **Scalar API Reference**: Interactive API documentation browser at `/scalar/v1`

## Run

```bash
dotnet run --project samples/ProDispatch.Examples.MinimalApi/ProDispatch.Examples.MinimalApi.csproj
```

The API will start on `http://localhost:5000`.

## Test Endpoints

### Health Check
```bash
curl http://localhost:5000/health
```

### Create User
```bash
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"userName":"john_doe","email":"john@example.com"}'
```

### Get User
```bash
curl http://localhost:5000/users/00000000-0000-0000-0000-000000000001
```

### Invalid Request (Validation)
```bash
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"userName":"jane","email":"invalid-email"}'
```

## Viewing API Documentation

OpenAPI and Scalar are automatically configured for the **Development** environment. Once the API is running in development mode, open:

- **Scalar UI**: http://localhost:5000/scalar (interactive explorer)
- **OpenAPI JSON**: http://localhost:5000/openapi/v1.json (raw schema)

Scalar provides a beautiful, user-friendly interface to explore and test your API endpoints directly in the browser.

> **Note**: OpenAPI and Scalar mappings are only enabled when `app.Environment.IsDevelopment()` is true. This follows ASP.NET Core best practices to keep API documentation out of production.

## Architecture

The `Program.cs` file demonstrates:
- Setting up the dispatcher and service factory with multiple registrations
- Pipeline behavior registration (logging, validation, timing) with explicit ordering
- Multiple notification handlers for the same event (UserCreated)
- Exception handling for validation errors and missing resources
- OpenAPI documentation integration

### Pipeline Behaviors

Commands and queries execute through a pipeline of behaviors:

**Create User Command Pipeline:**
1. LoggingBehavior (outermost) - logs request type and duration
2. ValidationBehavior - validates IValidatable requests
3. TimingBehavior - measures execution time
4. CreateUserHandler (innermost) - publishes UserCreated notification

**Get User Query Pipeline:**
1. LoggingBehavior - logs request type and duration
2. TimingBehavior - measures execution time
3. GetUserByIdHandler (innermost) - retrieves from in-memory store

### Notifications

When a user is created, the `UserCreated` notification is published to:
- `UserCreatedNotificationHandler` - logs user creation
- `SendWelcomeEmailNotificationHandler` - simulates sending a welcome email

- Registering handlers and behaviors
- Wiring dispatcher into ASP.NET Core dependency injection (via constructor parameters)
- Using ProDispatch in minimal API endpoint handlers
