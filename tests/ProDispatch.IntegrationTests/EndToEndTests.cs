using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Dispatcher;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Abstractions.Queries;
using ProDispatch.Abstractions.Exceptions;
using ProDispatch.Abstractions.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace ProDispatch.IntegrationTests;

public class EndToEndTests
{
    [Fact]
    public async Task Executes_command_notification_and_query()
    {
        List<string> events = [];
        InMemoryUserStore store = new();
        ServiceCollection services = new();
        services.AddSingleton(store);
        services.AddSingleton(events);
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(EndToEndTests).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ProDispatch.Tests.DispatcherTests).Assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddBehavior<TrackingBehavior>();
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        CreateUser command = new("alice", "alice@example.com");
        await dispatcher.SendAsync(command, TestContext.Current.CancellationToken);

        UserDto user = await dispatcher.SendAsync(new GetUserById(store.LastUserId), TestContext.Current.CancellationToken);

        Assert.Equal("alice", user.UserName);
        Assert.Contains("notification:alice", events);
        Assert.Contains("behavior:create:enter", events);
        Assert.Contains("behavior:create:exit", events);
    }

    private sealed record CreateUser(string UserName, string Email) : ICommand, ProDispatch.Abstractions.Validation.IValidatable
    {
        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(UserName)) yield return "UserName is required";
            if (string.IsNullOrWhiteSpace(Email)) yield return "Email is required";
            else if (!Email.Contains("@")) yield return "Email must be a valid email address";
        }
    }

    private sealed class CreateUserHandler(InMemoryUserStore store, IDispatcher dispatcher) : ICommandHandler<CreateUser>
    {
        public async Task HandleAsync(CreateUser command, CancellationToken cancellationToken = default)
        {
            Guid id = Guid.NewGuid();
            store.Add(new UserDto(id, command.UserName, command.Email));
            await dispatcher.PublishAsync(new UserCreated(id, command.UserName), cancellationToken);
        }
    }

    private sealed record GetUserById(Guid Id) : IQuery<UserDto>;

    private sealed class GetUserByIdHandler(InMemoryUserStore store) : IQueryHandler<GetUserById, UserDto>
    {
        public Task<UserDto> HandleAsync(GetUserById query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(store.Get(query.Id));
        }
    }

    private sealed record UserCreated(Guid Id, string UserName) : INotification;

    private sealed class UserCreatedHandler(List<string> events) : INotificationHandler<UserCreated>
    {
        public Task HandleAsync(UserCreated notification, CancellationToken cancellationToken = default)
        {
            events.Add($"notification:{notification.UserName}");
            return Task.CompletedTask;
        }
    }

    private sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            CancellationToken cancellationToken,
            Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            var requestType = request?.GetType().Name ?? "Unknown";
            DateTime startTime = DateTime.UtcNow;

            System.Console.WriteLine($"[LOG] Starting request: {requestType}");

            try
            {
                TResponse response = await next(request!, cancellationToken);
                TimeSpan duration = DateTime.UtcNow - startTime;
                System.Console.WriteLine($"[LOG] Completed request: {requestType} (Duration: {duration.TotalMilliseconds}ms)");
                return response;
            }
            catch (Exception ex)
            {
                TimeSpan duration = DateTime.UtcNow - startTime;
                System.Console.WriteLine($"[LOG] Failed request: {requestType} (Duration: {duration.TotalMilliseconds}ms, Error: {ex.Message})");
                throw;
            }
        }
    }

    private sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            CancellationToken cancellationToken,
            Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            if (request is IValidatable validatable)
            {
                List<string> errors = validatable.Validate().ToList();
                if (errors.Count > 0)
                {
                    throw new ValidationException(errors);
                }
            }

            return await next(request!, cancellationToken);
        }
    }

    private sealed class TrackingBehavior(List<string> events) : IPipelineBehavior<CreateUser, object>
    {
        public async Task<object> HandleAsync(CreateUser request, CancellationToken cancellationToken, Func<CreateUser, CancellationToken, Task<object>> next)
        {
            events.Add("behavior:create:enter");
            var result = await next(request, cancellationToken);
            events.Add("behavior:create:exit");
            return result;
        }
    }

    private sealed class InMemoryUserStore
    {
        private readonly Dictionary<Guid, UserDto> _users = new();

        public Guid LastUserId { get; private set; }

        public void Add(UserDto user)
        {
            _users[user.Id] = user;
            LastUserId = user.Id;
        }

        public UserDto Get(Guid id) => _users[id];
    }

    private sealed record UserDto(Guid Id, string UserName, string Email);
}