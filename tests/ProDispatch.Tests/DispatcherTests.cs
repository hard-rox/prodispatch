using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Exceptions;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Behaviors;
using ProDispatch.Dispatcher;
using ProDispatch.ServiceFactory;

namespace ProDispatch.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task SendAsync_CommandWithoutResult_InvokesHandler()
    {
        List<string> log = [];
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<TestCommand>>(() => new TestCommandHandler(log));
        InProcessDispatcher dispatcher = new(factory);

        await dispatcher.SendAsync(new TestCommand("ping"), CancellationToken.None);

        Assert.Single(log, "handled:ping");
    }

    [Fact]
    public async Task SendAsync_CommandWithPipeline_RunsBehaviorsInReverseRegistrationOrder()
    {
        List<string> log = [];
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<TestCommand>>(() => new TestCommandHandler(log));
        factory.Register(typeof(IPipelineBehavior<TestCommand, object>), () => new RecordingBehavior<TestCommand>(log, "outer"));
        factory.Register(typeof(IPipelineBehavior<TestCommand, object>), () => new RecordingBehavior<TestCommand>(log, "inner"));
        InProcessDispatcher dispatcher = new(factory);

        await dispatcher.SendAsync(new TestCommand("order"), CancellationToken.None);

        Assert.Equal(new[] { "behavior:outer:enter", "behavior:inner:enter", "handled:order", "behavior:inner:exit", "behavior:outer:exit" }, log);
    }

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsValue()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<ResultCommand, int>>(() => new ResultCommandHandler());
        InProcessDispatcher dispatcher = new(factory);

        var result = await dispatcher.SendAsync(new ResultCommand(41), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllHandlers()
    {
        List<string> log = [];
        SimpleServiceFactory factory = new();
        factory.Register<INotificationHandler<TestNotification>>(() => new RecordingNotificationHandler(log, "first"));
        factory.Register<INotificationHandler<TestNotification>>(() => new RecordingNotificationHandler(log, "second"));
        InProcessDispatcher dispatcher = new(factory);

        await dispatcher.PublishAsync(new TestNotification("event"), CancellationToken.None);

        Assert.Equal(new[] { "first:event", "second:event" }, log);
    }

    [Fact]
    public async Task ValidationBehavior_ThrowsOnInvalidRequest()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<ValidatedCommand>>(() => new ValidatedCommandHandler());
        factory.Register(typeof(IPipelineBehavior<ValidatedCommand, object>), () => new ValidationBehavior<ValidatedCommand, object>());
        InProcessDispatcher dispatcher = new(factory);

        Func<Task> act = () => dispatcher.SendAsync(new ValidatedCommand(string.Empty));

        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task ValidationBehavior_AllowsValidRequest()
    {
        SimpleServiceFactory factory = new();
        factory.Register<ICommandHandler<ValidatedCommand>>(() => new ValidatedCommandHandler());
        factory.Register(typeof(IPipelineBehavior<ValidatedCommand, object>), () => new ValidationBehavior<ValidatedCommand, object>());
        InProcessDispatcher dispatcher = new(factory);

        await dispatcher.SendAsync(new ValidatedCommand("ok"), CancellationToken.None);
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

    private sealed class RecordingBehavior<TRequest>(List<string> log, string name) : IPipelineBehavior<TRequest, object>
    {
        public async Task<object> HandleAsync(TRequest request, CancellationToken cancellationToken, Func<TRequest, CancellationToken, Task<object>> next)
        {
            log.Add($"behavior:{name}:enter");
            var result = await next(request, cancellationToken);
            log.Add($"behavior:{name}:exit");
            return result;
        }
    }

    private sealed record ResultCommand(int Value) : ICommand<int>;

    private sealed class ResultCommandHandler : ICommandHandler<ResultCommand, int>
    {
        public Task<int> HandleAsync(ResultCommand command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(command.Value + 1);
        }
    }

    private sealed record TestNotification(string Name) : INotification;

    private sealed class RecordingNotificationHandler(List<string> log, string id) : INotificationHandler<TestNotification>
    {
        public Task HandleAsync(TestNotification notification, CancellationToken cancellationToken = default)
        {
            log.Add($"{id}:{notification.Name}");
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
}
