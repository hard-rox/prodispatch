using FluentAssertions;
using ProDispatch.Abstractions.Commands;
using ProDispatch.Abstractions.Exceptions;
using ProDispatch.Abstractions.Notifications;
using ProDispatch.Abstractions.Pipeline;
using ProDispatch.Abstractions.Queries;
using ProDispatch.Behaviors;
using ProDispatch.Dispatcher;
using ProDispatch.ServiceFactory;

namespace ProDispatch.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task SendAsync_CommandWithoutResult_InvokesHandler()
    {
        var log = new List<string>();
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<TestCommand>>(() => new TestCommandHandler(log));
        var dispatcher = new InProcessDispatcher(factory);

        await dispatcher.SendAsync(new TestCommand("ping"));

        log.Should().ContainSingle("handled:ping");
    }

    [Fact]
    public async Task SendAsync_CommandWithPipeline_RunsBehaviorsInReverseRegistrationOrder()
    {
        var log = new List<string>();
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<TestCommand>>(() => new TestCommandHandler(log));
        factory.Register(typeof(IPipelineBehavior<TestCommand, object>), () => new RecordingBehavior<TestCommand>(log, "outer"));
        factory.Register(typeof(IPipelineBehavior<TestCommand, object>), () => new RecordingBehavior<TestCommand>(log, "inner"));
        var dispatcher = new InProcessDispatcher(factory);

        await dispatcher.SendAsync(new TestCommand("order"));

        log.Should().ContainInOrder("behavior:outer:enter", "behavior:inner:enter", "handled:order", "behavior:inner:exit", "behavior:outer:exit");
    }

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsValue()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<ResultCommand, int>>(() => new ResultCommandHandler());
        var dispatcher = new InProcessDispatcher(factory);

        var result = await dispatcher.SendAsync(new ResultCommand(41));

        result.Should().Be(42);
    }

    [Fact]
    public async Task PublishAsync_InvokesAllHandlers()
    {
        var log = new List<string>();
        var factory = new SimpleServiceFactory();
        factory.Register<INotificationHandler<TestNotification>>(() => new RecordingNotificationHandler(log, "first"));
        factory.Register<INotificationHandler<TestNotification>>(() => new RecordingNotificationHandler(log, "second"));
        var dispatcher = new InProcessDispatcher(factory);

        await dispatcher.PublishAsync(new TestNotification("event"));

        log.Should().BeEquivalentTo(new[] { "first:event", "second:event" });
    }

    [Fact]
    public async Task ValidationBehavior_ThrowsOnInvalidRequest()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<ValidatedCommand>>(() => new ValidatedCommandHandler());
        factory.Register(typeof(IPipelineBehavior<ValidatedCommand, object>), () => new ValidationBehavior<ValidatedCommand, object>());
        var dispatcher = new InProcessDispatcher(factory);

        var act = async () => await dispatcher.SendAsync(new ValidatedCommand(string.Empty));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ValidationBehavior_AllowsValidRequest()
    {
        var factory = new SimpleServiceFactory();
        factory.Register<ICommandHandler<ValidatedCommand>>(() => new ValidatedCommandHandler());
        factory.Register(typeof(IPipelineBehavior<ValidatedCommand, object>), () => new ValidationBehavior<ValidatedCommand, object>());
        var dispatcher = new InProcessDispatcher(factory);

        await dispatcher.SendAsync(new ValidatedCommand("ok"));
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
