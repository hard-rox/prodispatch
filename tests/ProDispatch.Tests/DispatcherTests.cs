using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Exceptions;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using ProDispatch.Abstractions.Dispatcher;
using ProDispatch.DependencyInjection;

namespace ProDispatch.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task SendAsync_CommandWithoutResult_InvokesHandler()
    {
        List<string> log = [];
        using ServiceProvider provider = BuildProvider(log);
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new TestCommand("ping"), CancellationToken.None);

        Assert.Single(log, "handled:ping");
    }

    [Fact]
    public async Task SendAsync_CommandWithPipeline_RunsFirstRegisteredOutermost()
    {
        List<string> log = [];
        using ServiceProvider provider = BuildProvider(log, cfg =>
        {
            cfg.AddBehavior<OuterRecordingBehavior>();
            cfg.AddBehavior<InnerRecordingBehavior>();
        });
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new TestCommand("order"), CancellationToken.None);

        Assert.Equal(new[] { "behavior:outer:enter", "behavior:inner:enter", "handled:order", "behavior:inner:exit", "behavior:outer:exit" }, log);
    }

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsValue()
    {
        using ServiceProvider provider = BuildProvider([]);
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new ResultCommand(41), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllHandlers()
    {
        List<string> log = [];
        using ServiceProvider provider = BuildProvider(log);
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.PublishAsync(new TestNotification("event"), CancellationToken.None);

        Assert.Equal(new[] { "first:event", "second:event" }, log.Order());
    }

    [Fact]
    public async Task ValidationBehavior_ThrowsOnInvalidRequest()
    {
        using ServiceProvider provider = BuildProvider([], cfg => cfg.AddBehavior<ValidationBehavior<ValidatedCommand, object>>());
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        Func<Task> act = () => dispatcher.SendAsync(new ValidatedCommand(string.Empty));

        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task ValidationBehavior_AllowsValidRequest()
    {
        using ServiceProvider provider = BuildProvider([], cfg => cfg.AddBehavior<ValidationBehavior<ValidatedCommand, object>>());
        IDispatcher dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new ValidatedCommand("ok"), CancellationToken.None);
    }

    private static ServiceProvider BuildProvider(List<string> log, Action<ProDispatchServiceConfiguration>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton(log);
        services.AddProDispatch(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DispatcherTests).Assembly);
            configure?.Invoke(cfg);
        });
        return services.BuildServiceProvider();
    }

    private sealed record TestCommand(string Payload) : ICommand;

    private sealed class TestCommandHandler(List<string> log) : ICommandHandler<TestCommand>
    {
        public Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            log.Add($"handled:{command.Payload}");
            return Task.CompletedTask;
        }
    }

    private abstract class RecordingBehavior<TRequest>(List<string> log, string name) : IPipelineBehavior<TRequest, object>
    {
        public async Task<object> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<object>> next)
        {
            log.Add($"behavior:{name}:enter");
            var result = await next(request, cancellationToken);
            log.Add($"behavior:{name}:exit");
            return result;
        }
    }

    private sealed class OuterRecordingBehavior(List<string> log) : RecordingBehavior<TestCommand>(log, "outer");

    private sealed class InnerRecordingBehavior(List<string> log) : RecordingBehavior<TestCommand>(log, "inner");

    private sealed record ResultCommand(int Value) : ICommand<int>;

    private sealed class ResultCommandHandler : ICommandHandler<ResultCommand, int>
    {
        public Task<int> HandleAsync(ResultCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(command.Value + 1);
        }
    }

    private sealed record TestNotification(string Name) : INotification;

    private sealed class RecordingNotificationHandler(List<string> log) : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            log.Add($"first:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondRecordingNotificationHandler(List<string> log) : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            log.Add($"second:{notification.Name}");
            return Task.CompletedTask;
        }
    }

    private sealed record ValidatedCommand(string Payload) : ICommand, ProDispatch.Abstractions.Validation.IValidatable
    {
        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(Payload))
            {
                yield return "Payload is required";
            }
        }
    }

    private sealed class ValidatedCommandHandler : ICommandHandler<ValidatedCommand>
    {
        public Task HandleAsync(ValidatedCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            CancellationToken cancellationToken,
            Func<TRequest, CancellationToken, Task<TResponse>> next)
        {
            if (request is ProDispatch.Abstractions.Validation.IValidatable validatable)
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
}